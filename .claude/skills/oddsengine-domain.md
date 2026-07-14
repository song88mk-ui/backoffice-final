# OddsEngine Domain

Domain knowledge for the odds engine integration layer — how the Backoffice interacts with the external odds engine, what messages it exchanges, and what configuration it manages on the engine's behalf. Apply this when working with trading configuration, odds sync, or the `TradingRepository`.

---

## What the OddsEngine is

The OddsEngine (OE) is an external service — it is **not** a project in this solution. The Backoffice acts as its configuration and management interface. The OE receives configuration from Backoffice via RabbitMQ and pushes odds updates back via the same broker.

## RabbitMQ routing key convention

All messages bound for the OddsEngine use the routing key format:
```
r.oe.{topic}.{messagetype}
```

All lowercase. Examples:
```
r.oe.cachesync.onmessagetype
r.oe.config.onupdateclientmarketconfig
r.oe.config.onupdateclientleagueconfig
r.oe.config.ondeletecache
```

The `r.oe.` prefix routes the message to the OddsEngine exchange. Messages without this prefix stay within Backoffice infrastructure.

## Messages sent TO the OddsEngine

| Message class | Trigger | Effect on OE |
|---------------|---------|-------------|
| `OnUpdateClientMarketConfigMessage` | Client market config change | OE reloads market rules for client |
| `OnUpdateClientLeagueConfigMessage` | Client league config change | OE reloads league rules for client |
| `OnDeleteCacheMessage` | Any config mutation | OE clears its config cache for the key |

All three are defined in `Backoffice.Shared/Models/OutgoingEngineMessages/`. Always include `CorrelationId` — the OE uses it for tracing.

## Messages received FROM the OddsEngine

The OE publishes odds and fixture events; Backoffice background services consume them:

| Consumer | Message types consumed | Project |
|----------|----------------------|---------|
| `Backoffice.Config.Service` | Config acknowledgements, fixture status changes | Config.Service |
| `Backoffice.Utility` | Cache invalidation triggers, export requests | Utility |
| `Backoffice.WebSocket.Service` | Real-time odds updates, fixture state changes | WebSocket.Service |

## TradingRepository

`TradingRepository` maps Backoffice entities to OddsEngine identifiers. The OE uses its own integer keys that differ from Backoffice IDs:

| Backoffice entity | OE identifier field | Notes |
|-------------------|--------------------|-------|
| Sport | `OddsEngineSportId` | Looked up via TradingContext |
| League | `OddsEngineLeagueId` | |
| Market type | `OddsEngineMarketId` | |
| Selection | `OddsEngineSelectionId` | |

When sending a config message to the OE, always use OE identifiers — never Backoffice internal IDs:

```csharp
var oeLeagueId = await _tradingRepository.GetOddsEngineLeagueIdAsync(backofficeLeagueId);

await _rabbitMqRepository.Publish(new OnUpdateClientLeagueConfigMessage
{
    OddsEngineLeagueId = oeLeagueId,  // OE identifier, not BackofficeLeagueId
    ClientId = clientId,
    Margin = margin,
    CorrelationId = Guid.NewGuid().ToString().ToUpper()
}, region: client.RegionCode);
```

## Config synchronisation model

The OE does not poll for config — it relies on messages from Backoffice. If a message is lost (fire-and-forget), the OE may have stale config until the next mutation triggers a resync.

Manual resync: there is a "force resync" API endpoint that republishes all current config for a client. Check `BackofficeRepository` for the method. Use it during incident recovery, not as a normal flow.

## Odds storage format

Odds values exchanged with the OE are stored as `long` in milliodds (odds × 1000):
- `1500` = 1.5 decimal odds
- `2000` = 2.0 decimal odds (evens)
- `3500` = 3.5 decimal odds

This is consistent with the Sportsbook domain — the representation is the same across all layers.

## Margin application order

When the OE applies client margins to base odds:

```
1. Apply client default margin (ClientBetSetting, no market/league override)
2. If league override exists → use league margin instead
3. If market override exists → use market margin instead (most specific wins)
4. Resulting odds transmitted to end-user
```

The Backoffice does not compute final odds — it only manages the margin configuration that the OE uses.

## Key constraints

- **Backoffice does not own OE uptime** — if the OE is down, messages queue in RabbitMQ but are not retried automatically.
- **Message order is not guaranteed** — if two config updates are published in rapid succession, the OE may apply them out of order. Design mutations to be idempotent where possible.
- **No OE read API** — the Backoffice cannot query the OE's current state. The source of truth for client config is always `BackofficeContext`.
