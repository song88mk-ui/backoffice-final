---
name: oddsengine-domain
description: How Backoffice interacts with the OddsEngine: the messages it sends and receives, the routing keys it uses, the config it manages on the engine's behalf, and the OddsEngine ID mappings in TradingContext. Use when working on trading configuration, margins, mappings or anything that publishes to r.oe.*, r.bo.config.* or r.uof.*.
---

# OddsEngine Domain

How backoffice-final talks to the OddsEngine (the separate `odds-engine-2` repo): the three publish paths, the routing keys and message classes it uses, who actually consumes them, and the Trading mapping tables. Apply it before changing any `MessageRequest`, any class under `Backoffice.Shared/Outgoing*` or `Incoming*`, `Backoffice.Config.Service`, or trading configuration screens. All paths below are relative to `backoffice-final/` unless prefixed with another repo.

## Exchange and publish paths

The three consumer configs declare the topic exchange `e.central` (`Backoffice.Config.Service/appsettings.json:33`, `Backoffice.Utility/appsettings.json:30`, `Backoffice.WebSocket.Service/appsettings.json:30`). Messages reach it three ways:

| Path | Code | Notes |
|---|---|---|
| API -> HTTP broker gateway | `Backoffice.API/Repositories/RabbitMQRepository.cs:37` posts `v1/broker/publish` | Base URL `API:RabbitMqApi:{SA,HK}:Uri`; anything not `SA` goes to HK (`:94-97`). Route matches `platform-final/Platform/Platform.RabbitMQ.API/Controllers/IntegrationController.cs:20`. |
| Web -> API -> broker API | `Backoffice.Web/Services/TradingService.cs:978` posts `api/trading/publish`; `Backoffice.API/Controllers/Trading/TradingController.cs:788` passes the `x-region` header; `Backoffice.API/Repositories/TradingRepository.cs:1853` hands the message to `IRabbitMqRepository.Publish` (BO-42) | Base `{API:RabbitMqApi:{SA,HK}:Uri}v1/broker/publish`. It used to be forwarded to the Trading API (`api.oddsengine.svc`), which does not resolve on DEV or STG. |
| Direct AMQP | `Backoffice.Shared/Utilities/MessageBroker.cs:483-509` | Used by Config.Service and Utility. `Backoffice.Web/Services/ExcelService.cs:267` also calls it, but both callers of `ConvertExceltoBulkUpdateConfigMessage` are commented out (`ImportExportMarginModal.razor.cs:115,168`), so that path is dead. `DeliveryMode = 2`, `Type` = message type (`:492-493`). |

Every path is fire-and-forget. `RabbitMqRepository.Publish` converts HTTP failures and exceptions into an Error `BaseResponse` (`RabbitMQRepository.cs:45-70`), and `MessageBroker.Publish` swallows exceptions (`MessageBroker.cs:511-514`).

Routing keys are case sensitive. Queue bindings are lowercased (`MessageBroker.cs:301`) but published keys are not, and Config.Service publishes mixed-case keys (see below).

## Routing keys backoffice-final publishes

| Routing key | Message type / payload | Publisher | Consumer (grepped in this workspace) |
|---|---|---|---|
| `r.oe.cachesync.{messagetype}` | `GetMessageType(CacheType)` / `OnDeleteCacheMessage` | `Backoffice.API/Repositories/BackOfficeRepository.cs:2548-2554` | `platform-final` `Platform.CacheSync.Service` binds `r.oe.cachesync.#` (`appsettings.json:18`, cases at `Worker.cs:58-91`) |
| `r.oe.cachesync.ontranslationchange` | `OnTranslationChange` / `OnTranslationChangeMessage` | `BackOfficeRepository.cs:3739-3745` | same |
| `r.oe.cachesync.onstatictranslationchange` | `OnStaticTranslationChange` / `OnDeleteCacheMessage` | `BackOfficeRepository.cs:3755-3765` | same |
| `r.oe.cachesync.onmarketsortorderchange`, `.onsportsortorderchange` | sort order | `Backoffice.API/Repositories/SportsbookRepository.cs:260,587` (and others) | same |
| `r.oe.cachesync.onclientchange`, `.onparlaybonuschange` | client / parlay bonus | `Backoffice.API/Repositories/PlatformRepository.cs:97,672` | same |
| `r.oe.cachesync.onwebpropschange.{clientcode}` | webprops | `Backoffice.API/Repositories/WebPropsRepository.cs:181` | also `odds-engine-2` `OE.Websocket.Service` (`appsettings.json:32` binds `.s88`; `argocd/apps/oddsengine/websocket-service/prd.yaml:45` binds `.*`) |
| `r.oe.web.config.OnUpdateClientMarketConfig` | `OnUpdateClientMarketConfigMessage` | `Backoffice.Config.Service/Repository/BackofficeRepository.cs:90` | none found |
| `r.oe.web.config.OnUpdateClientLeagueConfig` | `OnUpdateClientLeagueConfigMessage` | `Backoffice.Config.Service/Repository/BackofficeRepository.cs:115-118` | none found |
| `r.oe.web.config.oncopyclientsettings2` | `OnCopyClientSettings2` | `Backoffice.Web/Components/OddsEngine/Configuration/ConfirmCopyClientSettingsModal.razor.cs:84` | none found |
| `r.oe.web.config.onbulkupdateconfig2` | `OnBulkUpdateConfig2` / `OnBulkUpdateConfigMessage` | `Backoffice.Web/Services/ExcelService.cs:267` (dead code, callers commented out) | none found |
| `r.oe.web.config.ongetmarginspreadsheet3` | `OnGetMarginSpreadsheet3` / `OnConfigMyTemplateExportMessage` | `Backoffice.Utility/Repository/BackofficeRepository.cs:539` | none found |
| `r.uof.onleagueconfig`, `r.uof.onmarketconfig` | `OnLeagueConfig`, `OnMarketConfig` | `Backoffice.Web/Components/OddsEngine/Configuration/ModifyLeagueTemplateModal.razor.cs:252-276` (the modal is opened only from the Unassigned tab since BO-128; the Assigned tab is read-only) | `odds-engine-2` `OE.Adapter.Service` binds `r.uof.#` (`appsettings.json:34`) but has no case for these types (`Worker.cs:145-189`) |
| `r.uof.onpriceboost` | `OnPriceBoost` | `Backoffice.Web/Components/OddsEngine/ManageBoost/EditBoostedOddsModal.razor.cs:173-174` (also `NextBoostModalBody`, `DeleteBoostedOddsModal`) | bound by `r.uof.#`, no case found |
| `r.uof.onbetsettlement` | `OnBetSettlement` | `Backoffice.API/Repositories/TradingRepository.cs:1279-1285` | `OE.Adapter.Service` `Worker.cs:163` |
| `r.bo.config.updateclientmargin`, `r.bo.config.deleteconfigtemplate` | `UpdateClientMargin`, `DeleteConfigTemplate` | `BackOfficeRepository.cs:2102-2108`, `:1805-1811` | `Backoffice.Config.Service` (`appsettings.json:36` binds `r.bo.config.#`) |
| `r.bo.mapping.onbulkupdate` | per category | `TradingRepository.cs:2384-2390`, region hard-coded `"HK"` | none found |

"None found" means no binding matching the key, `r.oe.web.config.*` or `r.oe.#` exists in any repo of this workspace (`argocd` included). There are no `r.oe.config.*` keys in code.

## Config.Service: margin fan-out

Config.Service dispatches on `BasicProperties.Type` (`Backoffice.Config.Service/Worker.cs:51-65`):

```csharp
case "UpdateClientMargin":
    var UpdateClientMarginPayload = JsonSerializer.Deserialize<long>(payload);
    _backofficeRepository.UpdateClientMargin(UpdateClientMarginPayload, _messageBroker);
```

`UpdateClientMargin(configTemplateId)` reads `ConfigTemplateAssigned` and `ConfigTemplateMargin` from the Backoffice DB (`Repository/BackofficeRepository.cs:45-49`), builds `ClientMarketConfigModel2` rows per client and league, and publishes one `OnUpdateClientMarketConfigMessage` per league group (`:83-91`), then a single `OnUpdateClientLeagueConfigMessage` (`:114-118`). Missing margins default to `T = 691200, M = 101` prematch and `T = 0, M = 101` live (`:36-37`). The correlation id is a Unix timestamp in seconds, not a GUID (`:90`, `:172-176`).

`DeleteConfigTemplate` only unassigns or removes `ConfigTemplateAssigned` rows (`:126-167`) and publishes nothing; the method ends with `// Margins, to follow` (`:169`). The API publishes this message before its own `SaveChangesAsync` (`BackOfficeRepository.cs:1805-1814`).

## Cache preload endpoint

`POST api/config/cachepreload` takes an `OnDeleteCacheMessage` and is `[AllowAnonymous]` (`Backoffice.API/Controllers/Backoffice/ConfigController.cs:301-303`). It requires `CacheKeys` plus a defined `LanguageCode` for `DynamicTranslation`, otherwise `CacheKey`, unless `PurgeAll` is set (`:310-334`). It republishes one cache-sync message; it is not a config resync. `CacheType` values without a case in `GetMessageType` throw `ArgumentOutOfRangeException` (`BackOfficeRepository.cs:3768-3781`). No config resync endpoint exists.

## Messages from the OddsEngine

`Backoffice.WebSocket.Service` binds `r.oe.sb.engine.onoddschange|r.oe.sb.engine.onbetstopfixture|r.oe.sb.engine.onbetstopglobal|r.oe.bm.engine.notifications` (`appsettings.json:33`) and dispatches `OnOddsChange`, `OnBetStopFixture`, `OnBetStopGlobal`, `OnNewNotification` into `IncomingSbEngineMessages` classes (`Worker.cs:149-169`). The `r.oe.sb.engine.*` keys are published by `odds-engine-2` `OE.Sportsbook.Service/Repositories/SportsbookRepository.cs:109,250,295`. Unverified: no live publisher of `r.oe.bm.engine.notifications` was found; the only ones are commented out in `Backoffice.Utility/Repository/BackofficeRepository.cs:210,311,394,494`.

`Backoffice.Web/Middleware/RabbitMQMiddleware.cs` handles `r.oe.bm.engine.*` with the `OutgoingEngineBookmakerMessages` classes (`:31`, `:52-64`), but no `UseMiddleware` registration for it exists.

## Message classes

- `Backoffice.Shared/OutgoingEngineMessages/`: 17 files, including `OnUpdateClientMarketConfigMessage` and `OnUpdateClientLeagueConfigMessage` (both only `ConfigSettings` lists, `:10`), `OnDeleteCacheMessage`, `OnTranslationChangeMessage`, `OnLeagueConfigMessage`, `OnMarketConfigMessage`, `OnBulkUpdateConfigMessage`, `OnCopyClientSettingsMessage`, `OnWebPropsChangeMessage`.
- `OutgoingEngineBookmakerMessages/`: `OnExternalOddsChangeMessage`, `OnExternalBetStopFixtureMessage`, `OnExternalBetStopGlobalMessage`.
- `IncomingSbEngineMessages/`: `IncomingOddsChangeMessage`, `IncomingBetStopFixtureMessage`, `IncomingBetStopGlobalMessage`.
- `OutgoingWebSocketMessages/`: outbound SignalR payloads, not engine messages.

These are hand-maintained copies of the other side's classes; nothing enforces the wire contract. Odds are `decimal` (`IncomingOddsChangeMessage.cs:151`, `ClientOdds` at `OnExternalOddsChangeMessage.cs:95-98`), not scaled `long` values.

## Trading mappings

`TradingContext` exposes `SportMapping`, `SegmentMapping`, `LeagueMapping`, `FixtureMapping`, `MarketMapping`, `SelectionMapping` (`Backoffice.Shared/Data/Trading/TradingContext.cs:77-82`). The entities key on the engine's own ids plus a JSON `In` column, e.g. `SpId`, `SpNa`, `In`, `ChTyId`, `Ti` (`Data/Trading/SQLEntities/SportMapping.cs:9-13`). There are no `OddsEngine*Id` fields. Bulk mapping edits go out as `r.bo.mapping.onbulkupdate`.

## Constraints

- Margin order and odds computation happen in the engine; backoffice-final only stores margins and publishes them. Unverified: the precedence between client, league and market margins.
- A lost message is not replayed: no outbox, no retry, no resync.
- Before renaming a message type or key, grep the consumer repo; the compiler will not catch it.
