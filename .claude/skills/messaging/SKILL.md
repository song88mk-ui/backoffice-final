---
name: messaging
description: How RabbitMQ is used in backoffice-final: the publish paths, the message classes, the routing keys on e.central, and the consumer workers. Use when adding a message type, debugging a message that never arrived, or tracing an async flow between Backoffice and the OddsEngine or platform services.
---

# Messaging

Repository-specific knowledge for RabbitMQ in backoffice-final: the publish paths, the message classes, the routing keys actually in use, and the consumer workers. Apply this when adding a message type, debugging a message that "never arrived", or tracing an async flow between Backoffice and the OddsEngine or platform services.

---

## Topology in one paragraph

All Backoffice services use one topic exchange, `e.central` (`MessageBroker:Exchange` in `Backoffice.Config.Service/appsettings.json:33`, `Backoffice.Utility/appsettings.json:30`, `Backoffice.WebSocket.Service/appsettings.json:30`). Low-level access is `Backoffice.Shared/Utilities/MessageBroker.cs` (raw `RabbitMQ.Client`), registered as a singleton `IMessageBroker` in Config.Service (`Program.cs:34`), Utility (`Program.cs:35`), WebSocket.Service (`Program.cs:38`) and Web (`Extensions/WebApplicationBuilderExtensions.cs:102`). Backoffice.API does **not** talk AMQP; it publishes over HTTP.

## Message class locations

In `Backoffice.Shared` (no `Models/` prefix):

| Folder | Contents |
|---|---|
| `OutgoingEngineMessages/` | `OnDeleteCacheMessage`, `OnUpdateClientMarketConfigMessage`, `OnUpdateClientLeagueConfigMessage`, `OnWebPropsChangeMessage`, `OnTranslationChangeMessage`, `OnBulkUpdateConfigMessage`, `OnCopyClientSettingsMessage`, `OnLeagueConfigMessage`, `OnMarketConfigMessage`, `OnConfigMyTemplateExportMessage`, activity-log and export request messages |
| `OutgoingEngineBookmakerMessages/` | `OnExternalOddsChangeMessage`, `OnExternalBetStopFixtureMessage`, `OnExternalBetStopGlobalMessage` |
| `OutgoingWebSocketMessages/` | `Outgoing*Message` payloads pushed to the SignalR hub |
| `IncomingSbEngineMessages/` | `IncomingOddsChangeMessage`, `IncomingBetStopFixtureMessage`, `IncomingBetStopGlobalMessage`, consumed by WebSocket.Service |

Some classes do not match their file names: `OnActivityLogMessage` is in `OnActivityLogListMessage.cs:15`, `OnGetMarketSettlementHistoryMessage` in `OnGetManualMarketSettlementHistoryMessage.cs:7`, `OnExportConfigMarginMessage` in `OnGetMarginSpreadsheetMessage.cs:7`.

The consuming repos (platform-final, odds-engine-2) keep **their own copies** of these classes. A field rename here compiles fine and breaks silently on the wire.

## Publish path 1: API over HTTP

`IRabbitMqRepository.Publish(MessageRequest request, string region = "hk")` (`Backoffice.API/Repositories/IRabbitMQRepository.cs:11`). `MessageRequest` has `Payload` (pre-serialized string), `MessageType`, `RoutingKey`, `CorrelationId` (`Backoffice.Shared/Models/Requests/MessageRequest.cs:9-12`).

`RabbitMqRepository.Publish` (`Backoffice.API/Repositories/RabbitMQRepository.cs:32-76`):

- POSTs the request as JSON to `v1/broker/publish` on a broker HTTP API (:37). Unverified which deployed service that is; `platform-final/Platform/Platform.RabbitMQ.API/Controllers/IntegrationController.cs:20` and `bet-genius/BG.RabbitMQ.API/Controllers/IntegrationController.cs:34` both expose that route.
- `region` only selects the base URL: `"SA"` (case-insensitive) uses config key `API:RabbitMqApi:SA:Uri`, anything else uses `API:RabbitMqApi:HK:Uri` (:92-98).
- Creates a `new HttpClient()` per call (:85); the injected `HttpClient` is unused.
- **Never throws.** Non-success status and exceptions both return `BaseResponse` with `StatusCode.Error` (:45-71).

There are 16 awaited call sites in `Backoffice.API/Repositories/` (SportsbookRepository 7, BackOfficeRepository 5, PlatformRepository 2, TradingRepository 1, WebPropsRepository 1). None assigns or reads the result, so a failed publish is invisible to the caller.

A second HTTP route exists: Web `TradingService.Publish` (`Backoffice.Web/Services/TradingService.cs:974`, POST at :978) posts to the API's `api/trading/publish` (`Backoffice.API/Controllers/Trading/TradingController.cs:788`, region from the `x-region` header), which hands it via `TradingRepository.Publish` (`TradingRepository.cs:1853`, BO-42) to `IRabbitMqRepository.Publish` (the broker API `v1/broker/publish`); it used to be forwarded to the Trading API. Web uses this for `r.uof.onpriceboost`, `r.uof.onleagueconfig`, `r.uof.onmarketconfig` and `r.oe.web.config.oncopyclientsettings2`; the API itself uses it for `r.bo.mapping.onbulkupdate` (`TradingRepository.cs:2724-2730`). The Boosted Settings save also publishes `OnNewNotification` on `r.oe.bm.engine.notifications` (`TradingRepository.PublishBoostedSettingsNotification`, BO-42).

Real example, `Backoffice.API/Repositories/BackOfficeRepository.cs:2102-2108`:

```csharp
await _rabbitMqRepository.Publish(new MessageRequest()
{
    CorrelationId = Guid.NewGuid().ToString(),
    MessageType = "UpdateClientMargin",
    RoutingKey = $"r.bo.config.updateclientmargin",
    Payload = JsonSerializer.Serialize(configTemplateId)
});
```

## Publish path 2: direct AMQP via IMessageBroker

`MessageBroker.Publish(payload, messageType, routingKey, correlationId)` sets `BasicProperties.Type = messageType`, `DeliveryMode = 2`, injects OpenTelemetry trace headers, and publishes to the configured exchange (`MessageBroker.cs:483-515`). It swallows every exception (`// throw?`, :511-514) and does not change the routing key's case. Live callers:

- Config.Service: `OnUpdateClientMarketConfig` and `OnUpdateClientLeagueConfig` (`Backoffice.Config.Service/Repository/BackofficeRepository.cs:90` and :115-118), both as a side effect of consuming `UpdateClientMargin`. Config.Service is a publisher as well as a consumer.
- Utility: `OnGetMarginSpreadsheet3` (`Backoffice.Utility/Repository/BackofficeRepository.cs:539`, inside `ProcessOnGetOeConfigTemplate`).

Dead code in Web: `ExcelService.cs:267` publishes `OnBulkUpdateConfig2`, but both callers of `ConvertExceltoBulkUpdateConfigMessage` are commented out (`ImportExportMarginModal.razor.cs:115,168`). `Middleware/RabbitMQMiddleware.cs:24` connects and forwards `r.oe.bm.engine.*` messages to the hub, but nothing calls `UseMiddleware<RabbitMQMiddleware>`, so it never runs.

## Routing keys in use

No single format, and not all lowercase. Prefixes seen in live code:

| Prefix | Examples | Publisher |
|---|---|---|
| `r.oe.cachesync.*` | `onmarketsortorderchange`, `onsportsortorderchange`, `onclientchange`, `onparlaybonuschange`, `ontranslationchange`, `onstatictranslationchange`, `{messagetype}` via `CachePreload` (`BackOfficeRepository.cs:2548-2554`) | API |
| `r.oe.cachesync.onwebpropschange.{x}` | see note below | API `WebPropsRepository.cs:173-182` |
| `r.oe.web.config.*` | `OnUpdateClientMarketConfig`, `OnUpdateClientLeagueConfig`, `ongetmarginspreadsheet3`, `oncopyclientsettings2` | Config.Service, Utility, Web (via the API's `api/trading/publish`) |
| `r.bo.config.*` | `updateclientmargin`, `deleteconfigtemplate` | API `BackOfficeRepository.cs:1805-1811,2102-2108` |
| `r.bo.mapping.onbulkupdate` | message type `OnSportBulkUpdate`, `OnSegmentBulkUpdate`, `OnLeagueBulkUpdate`, `OnFixtureBulkUpdate` (`TradingRepository.cs:2709-2712`) | API via the broker API |
| `r.uof.*` | `onbetsettlement`, `onpriceboost`, `onleagueconfig`, `onmarketconfig` | API `TradingRepository.cs:1651`, Web |

Casing is inconsistent: Config.Service publishes `r.oe.web.config.OnUpdateClientMarketConfig` and `r.oe.web.config.OnUpdateClientLeagueConfig` with capitals (`BackofficeRepository.cs:90,118`), while `MessageBroker` lowercases keys only when *binding* (`MessageBroker.cs:300-301`). Topic matching is case-sensitive. The string `r.oe.web.config` appears nowhere in the workspace outside backoffice-final; Unverified whether any consumer binds it through a wildcard.

Keys that are bound but have no live publisher anywhere in the workspace: `r.oe.bm.engine.notifications` (Utility publishers at `BackofficeRepository.cs:210,311,394,494` are commented out) and `r.bo.util.*` (see below). In odds-engine-2, the adapter-service binds `r.uof.#` (`argocd/apps/oddsengine/adapter-service/*.yaml`).

**OnWebPropsChange note.** The routing key is `r.oe.cachesync.onwebpropschange.{request.ClientCode.ToLower()}`, but Web fills `ClientCode` with the upper-cased client GUID (`Web/Pages/Administration/ClientSiteConfig.razor.cs:199`, options built at `ClientSiteConfig.razor:36-37`), which the API route copies into the request (`WebPropsController.cs:45`). odds-engine-2's `OE.Websocket.Service` resolves the client from the last key segment and only matches known client codes (`OE.Websocket.Service.Tests/ClientRegistryTests.cs:80-90`). Unverified at runtime, but a GUID suffix likely resolves to no client.

## Consumer pattern

Each worker calls `Connect()`, subscribes to `MessageReceived`, and switches on `BasicProperties.Type`. Real excerpt, `Backoffice.Config.Service/Worker.cs:48-66`:

```csharp
private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
{
    var payload = Encoding.UTF8.GetString(e.Body);
    var messageType = e.BasicProperties.Type;
    var correlationId = e.BasicProperties.CorrelationId;

    try
    {
        switch (messageType)
        {
            case "UpdateClientMargin":
                var UpdateClientMarginPayload = JsonSerializer.Deserialize<long>(payload);
                _backofficeRepository.UpdateClientMargin(UpdateClientMarginPayload, _messageBroker);
                break;
            case "DeleteConfigTemplate":
                var DeleteConfigTemplatePayload = JsonSerializer.Deserialize<ConfigTemplate>(payload);
                _backofficeRepository.DeleteConfigTemplate(DeleteConfigTemplatePayload);
                break;
        }
```

Facts: no switch has a `default` case (unknown types are silently ignored); exceptions are caught and logged inside the handler, so `MessageBroker.HandleMessage` still acks (`MessageBroker.cs:386-398`). The message is only nacked and requeued if the handler itself throws out. WebSocket.Service's handler is `async void` (`Worker.cs:130`).

| Service | Queue / binding (appsettings) | `MessageType` cases |
|---|---|---|
| Config.Service | `q.bo.config` / `r.bo.config.#` | `UpdateClientMargin` (payload is a bare `long`), `DeleteConfigTemplate` (`Worker.cs:56-65`) |
| Utility | `q.bo.util` / `r.bo.util.#` | `LogActivity`, `LogActivityList`, `ExportMessageSettlementHistory`, `ExportManualBetSettlementHistory`, `ExportMarketSettlementHistory`, `ExportOeConfigTemplate`, `ExportCancelBetHistory` (`Worker.cs:79-109`) |
| WebSocket.Service | `q.bo.websocket` / `r.oe.sb.engine.onoddschange\|onbetstopfixture\|onbetstopglobal\|r.oe.bm.engine.notifications` | `OnOddsChange`, `OnBetStopFixture`, `OnBetStopGlobal`, `OnNewNotification` (`Worker.cs:147-170`), queued to worker threads and pushed to the SignalR hub |

Utility does not consume `OnDeleteCacheMessage` or anything cache-related. Every Web publisher to `r.bo.util.export` is commented out (for example `Web/Pages/Administration/CancelBetHistory.razor.cs:83`), and no live code in the workspace publishes `LogActivity`/`LogActivityList` (the test at `Backoffice.API.Tests/MarketSettlement/TradingRepositorySettlementTests.cs:317` asserts `LogActivityList` is no longer published by manual settlement). The Utility cases, and therefore its `OnGetMarginSpreadsheet3` publish, may be dead.

Nothing in Backoffice consumes `r.oe.cachesync.*`; see caching.md.

## Adding a new message type

1. Add the class under the right `Backoffice.Shared/` folder above, and add the matching copy in the consuming repo.
2. From the API, publish via `IRabbitMqRepository.Publish`; from a worker, via `IMessageBroker.Publish`. Use lowercase routing keys.
3. If you need to know whether it was sent, check `result.StatusCode`; nothing else will tell you.
4. Add a `case` whose label exactly equals `MessageType` in each consuming worker, and make sure that service's `MessageBroker:RoutingKey` config (appsettings and `argocd/apps/corp-apps/*/{dev,stg,prd}.yaml`) binds the key.
5. `docs/06_Messaging.md` also describes messaging; keep it in sync.

## Serialization

Payloads are produced with `System.Text.Json.JsonSerializer` at every publish site listed above (the API repositories that import Newtonsoft alias `JsonSerializer` to System.Text.Json, e.g. `PlatformRepository.cs:21`), and consumers deserialize with `JsonSerializer.Deserialize<T>` using default (case-sensitive) options. Newtonsoft.Json is referenced by API, Web and Shared and used in `Backoffice.WebSocket.API/Hubs/BackofficeHub.cs` and several API repositories, but not for broker payloads. The `JsonStringEnumConverter` in `Backoffice.API/Startup.cs:81` applies to MVC responses only, so enums such as `CacheType` in broker payloads serialize as integers.

## CorrelationId

There is no enforced format. Sites use upper-case GUIDs (`SportsbookRepository.cs:258`), lower-case GUIDs (`BackOfficeRepository.cs:1807`), or a Unix timestamp string (`Config.Service/Repository/BackofficeRepository.cs:90`). Tracing actually relies on the OpenTelemetry headers `MessageBroker` injects (`MessageBroker.cs:505`) and the `broker-publish` activity in `RabbitMQRepository.cs:34`.
