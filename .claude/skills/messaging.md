# Messaging

Repository-specific knowledge for the Backoffice RabbitMQ event system — message classes, routing keys, publish patterns, and consumer switch blocks. Apply this when adding new event types, fixing missed messages, or tracing async flows between services.

---

## Message class location

All message classes live in `Backoffice.Shared/Models/OutgoingEngineMessages/`. Every project references `Backoffice.Shared`, so any service can deserialize any message type without duplicating the class.

Naming convention: `On{Verb}{Noun}Message`

```
OnDeleteCacheMessage
OnActivityLogMessage
OnActivityLogListMessage
OnUpdateClientMarketConfigMessage
OnUpdateClientLeagueConfigMessage
OnGetManualBetSettlementHistoryMessage
```

## RabbitMQ routing key format

```
r.oe.{topic}.{messagetype}
```

All lowercase. The `r.oe.` prefix routes to the OddsEngine exchange region:

```
r.oe.cachesync.onmessagetype
r.oe.config.onupdateclientmarketconfig
r.oe.config.onupdateclientleagueconfig
r.oe.config.ondeletecache
r.oe.activitylog.onactivitylog
r.oe.export.ongetmanualbetsettlementhistory
```

The `region` parameter passed to `Publish` selects the exchange region (e.g. `"HK"`, `"EU"`). Always pass the client's `RegionCode`, not a hardcoded string.

## Publish pattern

```csharp
await _rabbitMqRepository.Publish(new MessageRequest
{
    Payload = JsonSerializer.Serialize(dataObject),
    CorrelationId = Guid.NewGuid().ToString().ToUpper(),  // Always uppercase GUID
    MessageType = "UpdateClientMarketConfig",              // CamelCase, matches consumer switch
    RoutingKey = "r.oe.config.onupdateclientmarketconfig"
}, region: client.RegionCode);
```

The `MessageType` string must **exactly match** the `case` label in every consumer's switch block.

## Known issue: fire-and-forget

The current codebase ignores the return value of `Publish`:

```csharp
_ = _rabbitMqRepository.Publish(...);  // Result discarded — publish failures undetected
```

New code should `await` the publish call even though publish failures are not currently handled:

```csharp
await _rabbitMqRepository.Publish(...);
```

This at minimum surfaces `TaskCanceledException` and `BrokerUnreachableException` to the calling repository's `catch` block.

## Consumer pattern in Worker.cs

All three background services (`Config.Service`, `Utility`, `WebSocket.Service`) use a switch-on-`messageType` pattern in `Worker.cs`:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await _consumer.ConsumeAsync(async (messageType, payload, correlationId) =>
    {
        switch (messageType)
        {
            case "UpdateClientMarketConfig":
                var config = JsonSerializer.Deserialize<OnUpdateClientMarketConfigMessage>(payload);
                await _repository.UpdateClientMarketConfigAsync(config);
                break;

            case "DeleteCache":
                var cache = JsonSerializer.Deserialize<OnDeleteCacheMessage>(payload);
                _memCache.DeleteCache(cache.CacheKey);
                break;

            case "ActivityLog":
                var log = JsonSerializer.Deserialize<OnActivityLogMessage>(payload);
                await _repository.AddActivityLogAsync(log);
                break;

            default:
                _logger.LogWarning("Unhandled message type: {MessageType}", messageType);
                break;
        }
    }, stoppingToken);
}
```

Always include a `default` case that logs the unknown type — silent swallowing hides routing bugs.

## Adding a new message type — checklist

1. **Define class** in `Backoffice.Shared/Models/OutgoingEngineMessages/OnXxxMessage.cs`:
```csharp
public class OnXxxMessage
{
    public int ClientId { get; set; }
    public string CorrelationId { get; set; }
    // ... domain fields
}
```

2. **Publish** from `Backoffice.API` repository using the established pattern.

3. **Add consumer case** in every `Worker.cs` that should handle the type:
```csharp
case "Xxx":
    var msg = JsonSerializer.Deserialize<OnXxxMessage>(payload);
    await _repository.HandleXxxAsync(msg);
    break;
```

4. **Document** the new message type in `docs/06_Messaging.md`.

5. **Test** end-to-end: publish → verify consumer receives and processes.

## Serialization consistency

All message serialization uses `System.Text.Json.JsonSerializer`:

```csharp
// Publisher
Payload = JsonSerializer.Serialize(dataObject)

// Consumer
var data = JsonSerializer.Deserialize<T>(payload)
```

`Backoffice.API` has some legacy `Newtonsoft.Json` usage. New messages must use `System.Text.Json` to remain consistent with all consumers. Do not mix serializers on the same message type.

## Message types by consumer

| Consumer | Handles |
|----------|---------|
| `Config.Service` | `UpdateClientMarketConfig`, `UpdateClientLeagueConfig`, fixture/status sync messages |
| `Utility` | `DeleteCache`, `ActivityLog`, `ActivityLogList`, export trigger messages |
| `WebSocket.Service` | Real-time event messages forwarded to SignalR hub |

If unsure which service should consume a new type, check whether it involves: config sync → Config.Service, cache/audit/export → Utility, real-time UI push → WebSocket.Service.

## CorrelationId requirements

Every published message must include a `CorrelationId`:

```csharp
CorrelationId = Guid.NewGuid().ToString().ToUpper()
```

Uppercase format is required — the OddsEngine and Utility services expect it in this format for tracing. Lowercase GUIDs will not correlate correctly in logs.
