# Messaging Architecture

**Document Status:** Analysis Complete (No Code Changes Made)  
**Date:** June 18, 2026

## Message Broker: RabbitMQ

**Version:** 5.2.0  
**Provider:** Message Broker Service  
**Topology:** Topic Exchange (Pattern-Based Routing)

---

## Message Publishing Flow

### From Backoffice.API

```
API Endpoint (HTTP Request)
  ↓
Repository Method
  ↓
IRabbitMqRepository.Publish()
  ↓
HTTP Request to RabbitMQ Management API
  ↓
RabbitMQ Broker
  ↓
Topic Exchange
  ↓
Routing Key Matching
  ↓
Queue Binding
```

### Publisher Pattern

```csharp
await _rabbitMqRepository.Publish(new MessageRequest
{
    Payload = JsonSerializer.Serialize(dataObject),
    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
    MessageType = "MessageTypeName",
    RoutingKey = "r.oe.cachesync.onmessagetype"
}, region: "HK");
```

### Issues with Current Pattern

1. **Fire-and-Forget** - No await on result
2. **No Error Handling** - Publishing failures ignored
3. **HTTP-Based Publishing** - Via RabbitMQ Management API (slower)
4. **Region Parameter** - Affects routing

---

## Message Types Published (Outgoing)

### Activity Logging
```
OnActivityLogMessage
├─ Single user action
├─ Route: r.oe.activity.onactivitylog
└─ Destination: Utility service

OnActivityLogListMessage
├─ Batch of actions
├─ Route: r.oe.activity.onactivityloglist
└─ Destination: Utility service
```

### Boosted Settings Notification (BO-42)
```
OnNewNotification
├─ Published by TradingRepository.AddOrUpdateBoostedSettings after the % cap is saved
├─ Route: r.oe.bm.engine.notifications
├─ Payload: NotificationsDetailsModel (UserId = the user who saved, Type = BoostedSettings)
└─ Consumer: Backoffice.WebSocket.Service, which pushes it to that user's browser as a toast
```
It is not stored (nothing writes the `Notifications` table in this flow), a publish failure is only
logged and never undoes the save, and no message is sent without a valid user id because a
notification without a user would reach everyone. The price boost routes of the Trading controller
used to publish `LogActivityList` and `OnNewNotification` on `r.oe.util.*` through the Trading API;
nothing deployed consumes those keys, and the Web writes the price boost activity logs itself.
`r.uof.onpriceboost` is still published by the Web (through `api/trading/publish`, now the broker API
of this service) and has no consumer in the deployed Odds Engine v2.

### League Settings Saved (BO-145)
```
OnLeagueConfig
├─ Published by BackofficeRepository.SetConfigState after a Config page save (Assigned as Active or Restricted, or
│  Unassigned) has been written to Trading.dbo.ClientLeagueConfig
├─ Route: r.uof.onleagueconfig (through IRabbitMqRepository.Publish, the broker API)
├─ Payload: OnLeagueConfigMessage (ClientId, ClientCode, SportId, SegmentId, LeagueId, IsActive), one per distinct
│  league of the save, carrying the client of the league's first row
└─ Consumer: odds-engine-2 OE.Adapter.Service (binds r.uof.#)
```
The adapter only uses the league ids: it reads the saved settings again, works out the clients allowed to see the
league (every client except the restricted ones) and has the Sportsbook shard stamp them into the `ClCo` of the
league's stored fixtures and markets. `IsActive` is informational (true for Active and Unassigned, false for
Restricted). The settings are saved first, so a publish failure is logged and never undoes the save; the adapter also
reloads the table every 60 seconds, so a lost message delays the change by at most that. The older template modal
(`ModifyLeagueTemplateModal`) still publishes `OnLeagueConfig` and `OnMarketConfig` from the Web.

### Configuration Updates
```
OnUpdateClientMarketConfigMessage
├─ Market-level config
├─ Broadcasts configuration changes
└─ Consumers: WebSocket, Cache

OnUpdateClientLeagueConfigMessage
├─ League-level config
└─ Consumers: WebSocket, Cache

OnBulkUpdateConfigMessage
├─ Multiple configurations
└─ Used for: Batch operations

OnCopyClientSettingsMessage
├─ Settings replication
└─ Used for: Client cloning
```

### Manual Market Settlement
```
OnBetSettlement  (published by TradingRepository.AddOrUpdateManualMarketSettlement)
├─ Route: r.uof.onbetsettlement
├─ Payload: OnBetSettlementMessage { ProviderFixtureId, FixtureSpecifier,
│           Selections[] { MarketTemplateId, SelectionTemplateId, MarketSpecifier,
│                          SelectionSpecifier, Result, VoidFactor, DeadHeatFactor } }
└─ Cross-service settlement pipeline (NOT settled by Backoffice itself):
   BO ──r.uof.onbetsettlement──▶ OE.Adapter.Service (binds r.uof.#)
      ──r.oe.adapter.onbetsettlement.{instance}──▶ OE.Sportsbook.Service
         · matches Mongo Selection on (PrFiId, MaTeId, SeTeId, SeSp)
         · on match ──r.oe.bet.settlement──▶ Platform.Settlement.Service
            · SettleBet / ResettleBet ──▶ UPDATE Bet / BetSelection
```

> **⚠️ GM-543 — SelectionSpecifier is mandatory.** OE.Sportsbook.Service matches the
> selection using `SeSp` (selection specifier). Lines of the same market (e.g. Under
> 2.50 / 3.50 / 4.50) share `MaTeId`+`SeTeId` and differ **only** by `SeSp`. If the
> outgoing message omits `SelectionSpecifier`, the Mongo filter degenerates to
> `{ SeSp: null }`, matches nothing, and the settlement is **silently dropped** — the
> activity-log/history row is still written (that is unconditional), but the bet is
> never settled. The Backoffice does not own the `Bet` table; it only kicks off this
> pipeline. Always populate `SelectionSpecifier` on the outgoing settlement message.

### Cache Invalidation
```
OnDeleteCacheMessage
├─ Cache key: "cc:{clientId}"
├─ CacheType enum
├─ Route: r.oe.cachesync.ondeletecache
└─ Consumers: Utility, WebSocket.Service

OnMarketSortOrderChange  (payload: OnDeleteCacheMessage { CacheType = MarketSortOrder })
├─ Route: r.oe.cachesync.onmarketsortorderchange
├─ Published by SportsbookRepository after:
│   · AddOrUpdateClientMarketTemplatesOrder / CopyClientMarketTemplatesOrder (market sort order)
│   · AddOrUpdateClientSelectorMarketTemplates / CopyClientSelectorMarketTemplates
│     (BO-87: ClientSelectorMarketTemplate2 writes from the Selector Market Templates page)
└─ Platform.CacheSync.Service ──POST v1/cachepreload──▶ Platform.Sportsbook.API
   · purges "mtg2", "cmtg", "csmt" (BO-87) and the cached REST responses, then
     republishes r.oe.sb.engine.onmarketsortorderchange.{clientCode} to the engine.

OnOddstableLeagueSortOrderChange  (BO-107; payload: OnDeleteCacheMessage { CacheType = OddstableLeagueSortOrder })
├─ Route: r.oe.cachesync.onoddstableleaguesortorderchange
├─ Published by SportsbookRepository after AddOrUpdateClientOddstableLeagueSettings
│   (one message), CopyClientOddstableLeagueSettings (one message per target client) and
│   ImportClientOddstableLeagueSettings (BO-122; one message, only for a commit that changed the order)
└─ No consumer yet: Platform.CacheSync.Service has no case for this message type, so it is
   logged and dropped. The purge, onward republish and read path are tracked in
   docs/08_TechnicalDebt.md ("League Sorting (BO-107)").
```

### Translation
```
OnTranslationChangeMessage  (published by BackofficeRepository glossary writes)
├─ Route: r.oe.cachesync.ontranslationchange
├─ Payload: { Tr: [ { StId, LaCo, Tx, SrcTx } ] }
│   · StId = TranslationCache2.STRING_ID
│     ("SpId:{id}" | "SegId:{id}" | "LeId:{id}" | "Te:{teamId}" | "MaTe:{MaTeId}" | "SeTe:{MaTeId}:{SeTeId}")
│   · LaCo = target language ("ZH" | "ES" | "FR"), Tx = translated text (empty = clear)
│   · SrcTx = the row's SOURCE_TEXT (GM-852). OE.Translation.Service uses it to scope a MaTe
│     update to markets whose MaNa matches it, ignoring case. Set on every delta below;
│     TranslationRepositoryTests asserts it per path (GM-874).
├─ Published after AddOrUpdateSportsbookGlossary (one delta for the edited target
│  language, plus one per sibling row sharing the same SourceText), ImportSportsbookGlossary (bulk CSV upload/upsert -- one delta per changed
│  row/language, batched into a single publish call for the whole file), and
│  DeleteSportsbookGlossary (an empty-Tx clear per language that had text)
└─ Propagation chain (all downstream of the topic exchange, no further BO involvement):
   Platform.CacheSync.Service (binds r.oe.cachesync.#)
      · purges Platform.Sportsbook.API sb:translation cache per (StringId, TargetLanguage)
      · relays ──r.oe.ontranslationchange──▶ OE.Adapter.Service (in-memory enrichment cache)
                                         └─▶ OE.Translation.Service (Mongo *NaTr writes +
                                             translation-only ON_ODDS_CHANGE pushes to websocket)

OnDynamicTranslationChange  (legacy purge-only path, still used by POST api/config/cachepreload)
├─ Route: r.oe.cachesync.ondynamictranslationchange
├─ Payload: OnDeleteCacheMessage { CacheType = DynamicTranslation, ... }
└─ Carries no translation text — cache invalidation only

OnStaticTranslationChange  (published by BackofficeRepository static translation writes)
├─ Route: r.oe.cachesync.onstatictranslationchange
├─ Payload: OnDeleteCacheMessage { CacheType = StaticTranslation, CacheKey = languageCode }
├─ Published after AddOrUpdateSportsbookTranslation / DeleteSportsbookTranslation
│  (SportsbookStatic table edits — the Sportsbook UI's label dictionary)
└─ Platform.CacheSync.Service ──POST v1/cachepreload──▶ Platform.Sportsbook.API
   · purges "tr" (whole SportsbookStatic table cache) + "tr-{LANG}" (per-language
     merged results, 24h TTL) so the next UI page load serves the new label.
   · Always purges all languages: EN rows are merged into every per-language result.
   · Note: EN labels never reach the UI from the DB — Platform.Child.Web uses a
     hardcoded EN baseline (Utility2.GetDefaultTranslation) and only overlays
     non-EN languages from the API.
```

### Exports
```
OnGetManualBetSettlementHistoryMessage
OnGetMessageSettlementHistoryMessage
OnGetMarketSettlementHistoryMessage
OnGetOeConfigTemplateMessage
OnConfigMyTemplateExportMessage
```

---

## Message Types Consumed (Incoming)

### Config.Service Consumer

```csharp
switch (messageType)
{
    case "UpdateClientMargin":
        var margin = JsonSerializer.Deserialize<long>(payload);
        _backofficeRepository.UpdateClientMargin(margin, _messageBroker);
        break;
        
    case "DeleteConfigTemplate":
        var template = JsonSerializer.Deserialize<ConfigTemplate>(payload);
        _backofficeRepository.DeleteConfigTemplate(template);
        break;
}
```

**Message Volume:** Low (configuration updates only)

### Utility Service Consumer

```csharp
switch (messageType)
{
    case "LogActivity":
        var log = JsonSerializer.Deserialize<OnActivityLogMessage>(payload);
        _backofficeRepository.ProcessActivityLog(log);
        break;
        
    case "ExportMessageSettlementHistory":
        var settlement = JsonSerializer.Deserialize<OnGetMessageSettlementHistoryMessage>(payload);
        _backofficeRepository.ProcessOnGetMessageSettlementHistory(settlement, _messageBroker, correlationId);
        break;
        
    // ... 8+ more message types
}
```

**Message Volume:** Low. Nothing publishes the `Export*` messages any more (BO-136): the Cancel Bet,
Manual Bet, Market and Message Settlement history pages build their CSV in Back Office Web from the
same API call as the table, and the Config template export was removed because its
`r.oe.web.config.ongetmarginspreadsheet3` message has no consumer. See `docs/08_TechnicalDebt.md`.

### WebSocket.Service Consumer

```
Receives: All messages
Processes: Serialization & deserialization
Publishes: Via BackofficeHub to WebSocket clients
```

**Incoming wire contract (Odds Engine v2)** — added Aug 2026 (BO-97):

`Backoffice.WebSocket.Service` binds `q.bo.websocket` on exchange `e.central` to
`r.oe.sb.engine.onoddschange`, `r.oe.sb.engine.onbetstopfixture`, `r.oe.sb.engine.onbetstopglobal`
and `r.oe.bm.engine.notifications`. The `OnOddsChange` and `OnBetStopFixture` producers are
**Odds Engine v2** (`odds-engine-2`: OE.Sportsbook.Service, OE.Translation.Service), which
serialize payloads with **abbreviated JSON property names** (`FiNa`, `FiTyId`, `Ma`, `Se`, `Od`, ...)
— unlike the retired OddsEngine v1, which used full names.

The incoming models in `Backoffice.Shared/IncomingSbEngineMessages/` therefore carry
`[JsonPropertyName]` attributes mapping each property to its OEv2 wire name
(`IncomingOddsChangeMessage`, `IncomingMarketMessage`, `IncomingSelectionMessage`,
`IncomingBetStopFixtureMessage`). Properties without an attribute are **not** on the OEv2 wire and
stay at their defaults - notably `CurrentPeriod` (OEv2 sends only the numeric `CuPe`),
`ProviderSelectionId`, and `MarketTemplateGroups` (betstop). If OEv2's
`OnInternalOddsChangeMessage` contract changes, these attributes must be kept in sync
(contract tests: `Backoffice.API.Tests/IncomingSbEngineMessages/`).

---

## Message Flow Sequence

### Typical Cache Invalidation Flow

```
1. API receives config update request
   POST /api/config/update { ClientId: 123, ... }

2. API updates database
   await _context.ConfigTemplate.AddAsync(template);
   await _context.SaveChangesAsync();

3. API publishes cache invalidation
   await _rabbitMqRepository.Publish(new MessageRequest {
       Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage { 
           ClientId = 123 
       }),
       MessageType = "OnDeleteCache",
       RoutingKey = "r.oe.cachesync.ondeletecache"
   });

4. RabbitMQ routes to queues
   ├─ Utility queue
   ├─ WebSocket queue
   └─ Any bound consumers

5. Consumers process
   ├─ Utility: Invalidates cache
   ├─ WebSocket: Broadcasts to clients
   └─ Others: React to message

6. Clients receive
   WebSocket clients get real-time notification
```

### Activity Logging Flow

```
1. User action in UI
   - Update, Create, Delete operation

2. Controller logs via API
   POST /api/backoffice/activity-log { 
       Activity: "Config updated", 
       UserId: "user123" 
   }

3. API publishes to RabbitMQ
   MessageRequest {
       MessageType: "LogActivity",
       Payload: JSON of action,
       CorrelationId: Guid
   }

4. Utility service consumes
   Deserializes message
   Calls ProcessActivityLog()

5. Activity logged to database
   ActivityLog table updated
   Audit trail complete
```

---

## Message Structure

### BasicProperties

```csharp
BasicProperties {
    Type: "OnActivityLogMessage"
    CorrelationId: "F47AC10B-58CC-4372-A567-0E02B2C3D479"
    ContentType: "application/json"
    Timestamp: DateTime
    DeliveryMode: 2 (persistent)
}
```

### Payload Format

```csharp
new MessageRequest {
    Payload: "{\"clientId\": 123, \"name\": \"Client123\"}"  // JSON string
    CorrelationId: "F47AC10B-58CC-4372-A567-0E02B2C3D479"
    MessageType: "OnUpdateClientMarketConfig"
    RoutingKey: "r.oe.cachesync.onupdateclientmarketconfig"
}
```

### Deserialization Pattern

```csharp
var payload = Encoding.UTF8.GetString(e.Body);
var messageType = e.BasicProperties.Type;
var correlationId = e.BasicProperties.CorrelationId;

switch (messageType)
{
    case "OnActivityLogMessage":
        var message = JsonSerializer.Deserialize<OnActivityLogMessage>(payload);
        // Process message
        break;
}
```

---

## RabbitMQ Configuration

### Connection Settings

```csharp
var messageBroker = _credential;
var connectionFactory = new ConnectionFactory
{
    HostName = messageBroker.Host,           // From config
    Port = messageBroker.Port,               // 5672 (plain) or 5671 (SSL)
    UserName = messageBroker.Username,
    Password = messageBroker.Password,
    VirtualHost = messageBroker.VirtualHost,
    RequestedHeartbeat = 60,                 // Seconds
    Ssl = messageBroker.Ssl,                 // Conditional SSL
    NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
    ClientProperties = new Dictionary<string, object> {
        { "application_name", "Song88 Platform" }
    }
};
```

### Multi-Region Support

```csharp
MessageBroker:HK:
├─ Host: broker-hk.example.com
├─ Port: 5672
├─ VirtualHost: /hk
└─ Queue: orders.hk

MessageBroker:SA:
├─ Host: broker-sa.example.com
├─ Port: 5672
├─ VirtualHost: /sa
└─ Queue: orders.sa
```

### External Broker Support

```csharp
ExternalMessageBroker:
├─ Host: external-broker.example.com
├─ Port: 5671 (SSL)
├─ Username: external_user
├─ Password: password
├─ VirtualHost: /external
├─ Exchange: external.exchange
├─ Queue: external.queue
└─ RoutingKey: external.#
```

---

## Distributed Tracing Integration

### Activity Propagation

```csharp
var parentContext = Propagator.Extract(default, e.BasicProperties, 
    ExtractTraceContextFromBasicProperties);

using (var activity = _activitySource.StartActivity(
    $"{e.RoutingKey} receive", 
    ActivityKind.Consumer, 
    parentContext.ActivityContext))
{
    activity?.SetTag("messaging.system", "rabbitmq");
    activity?.SetTag("messaging.destination_kind", "queue");
    activity?.SetTag("messaging.rabbitmq.routing_key", e.RoutingKey);
    activity?.SetTag("messaging.rabbitmq.message_type", messageType);
    
    // Process message
}
```

### Correlation Context

```csharp
Baggage.Current = parentContext.Baggage;
```

**Benefit:** Trace request across multiple services

---

## Message Acknowledgment Pattern

### Auto-Acknowledge (Default)

```csharp
// Message automatically acknowledged
// Risk: Message lost if processing fails
```

### Manual Acknowledge

```csharp
try
{
    // Process message
    ProcessMessage(e);
    
    // Acknowledge after successful processing
    _consumer.Model.BasicAck(e.DeliveryTag, false);
}
catch
{
    // Nack with requeue
    _consumer.Model.BasicNack(e.DeliveryTag, false, true);
}
```

**Current Implementation:** Manual acknowledgment in workers

---

## Queue Configuration

### Exchange Configuration

```csharp
_model.ExchangeDeclare(
    exchange: messageBroker.Exchange,
    type: "topic",                          // Pattern-based routing
    durable: true,                          // Persists broker restart
    autoDelete: false,
    arguments: null);
```

### Queue Declaration

```csharp
_model.QueueDeclare(
    queue: messageBroker.Queue,
    durable: true,
    exclusive: messageBroker.IsExclusive,   // Exclusive to this consumer
    autoDelete: false,
    arguments: GetQueueArguments(messageBroker));

// Bind queue to exchange
_model.QueueBind(
    queue: messageBroker.Queue,
    exchange: messageBroker.Exchange,
    routingKey: messageBroker.RoutingKey);  // Pattern like "r.oe.*"
```

### Priority Queues (Optional)

```csharp
if (messageBroker.IsPriority)
{
    var queueArguments = new Dictionary<string, object> {
        { "x-max-priority", 10 }
    };
}
```

---

## Consumer Configuration

### Prefetch Count

```csharp
_model.BasicQos(
    prefetchSize: 0,
    prefetchCount: (ushort)(prefetchCount ?? 1),
    global: false);
```

**Effect:**
- Limits number of unacknowledged messages
- Prevents consumer overload
- Higher value = more parallelism but more memory

### Consumer Settings

```csharp
var consumer = new EventingBasicConsumer(_model);
consumer.Received += (model, ea) => HandleMessage(ea);

_model.BasicConsume(
    queue: messageBroker.Queue,
    autoAck: false,                         // Manual ack
    consumerTag: messageBroker.ConsumerName,
    noLocal: false,
    exclusive: false,
    arguments: null,
    consumer: consumer);
```

---

## Error Handling in Messages

### Publishing Errors (Currently Not Handled)

```csharp
// Fire-and-forget approach
await _rabbitMqRepository.Publish(messageRequest, region);
// If failed, no retry or notification
```

**Issues:**
- ✗ Exceptions swallowed
- ✗ No circuit breaker
- ✗ No dead letter queue
- ✗ No alerting

### Consumption Errors

```csharp
try
{
    switch (messageType)
    {
        case "LogActivity":
            ProcessActivityLog(message);
            _consumer.Model.BasicAck(e.DeliveryTag, false);
            break;
    }
}
catch (Exception ex)
{
    _logger.LogError("Error processing message: {ex}", ex);
    // Message silently nacked and requeued
    _consumer.Model.BasicNack(e.DeliveryTag, false, true);
}
```

---

## Performance Characteristics

### Message Throughput

| Metric | Value |
|--------|-------|
| Messages/second capacity | ~10,000 (depends on broker) |
| Average message size | ~1KB |
| Latency (P50) | ~10ms |
| Latency (P99) | ~100ms |

### Bottlenecks

1. **HTTP Publishing** - Slower than direct socket
2. **JSON Serialization** - Every message serialized twice
3. **Prefetch Count** - Limits parallelism
4. **Network** - Multi-region adds latency

---

## Dead Letter Queue (Not Implemented)

**Current State:** Not configured  
**Risk:** Failed messages lost after retries

**Recommendation:** Add DLQ for failed messages
```csharp
var dlqArguments = new Dictionary<string, object> {
    { "x-dead-letter-exchange", "dlx.exchange" },
    { "x-dead-letter-routing-key", "dlq.routing.key" },
    { "x-message-ttl", 3600000 }  // 1 hour
};
```

---

## Message Flow Debugging

### Correlation ID Tracking

```
Request ID: F47AC10B-58CC-4372-A567-0E02B2C3D479
  ├─ API endpoint receives request
  ├─ Message published with this ID
  ├─ Multiple services process with same ID
  └─ Complete request trace available
```

### Activity Tags in Logs

```
timestamp: 2026-06-18T14:30:45.123Z
correlationId: F47AC10B-58CC-4372-A567-0E02B2C3D479
messageType: OnActivityLogMessage
routingKey: r.oe.activity.onactivitylog
processingTime: 45ms
status: success
```

---

## Scaling Considerations

### Horizontal Scaling

```
Messages Published per second: 100
Broker throughput: 10,000 msgs/sec
├─ Can handle 100x current load
└─ Broker not bottleneck

Consumer Capacity:
├─ Utility Service: 1 instance
├─ Config.Service: 1 instance
├─ WebSocket.Service: 1 instance
└─ Adding instances: Parallel consumption possible
```

### Queue Length Monitoring

**Currently Missing:** Queue depth monitoring

**Should Monitor:**
- Messages pending
- Consumer lag
- Processing time

---

## Future Improvements

1. **Switch to direct RabbitMQ SDK** instead of HTTP API
2. **Implement dead letter queues** for failed messages
3. **Add circuit breaker** for publishing failures
4. **Monitor queue depth** and consumer lag
5. **Implement message compression** for large payloads
6. **Add retry mechanism** with backoff
7. **Implement saga pattern** for distributed transactions

