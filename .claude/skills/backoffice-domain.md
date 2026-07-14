# BackOffice Domain

Domain knowledge for the backoffice management layer — client administration, bet configuration, activity logging, and config template management. Apply this when working with `BackofficeContext`, `BackofficeRepository`, or `ClientController`.

---

## Core entities in BackofficeContext

| Entity | Purpose | Key fields |
|--------|---------|-----------|
| `ClientInformation` | A betting operator client | `ClientId`, `ClientName`, `Status`, `RegionCode` |
| `ClientBetSetting` | Per-client bet rules | `ClientId`, `MarketTypeId`, `MinBet`, `MaxBet`, `Margin` |
| `ConfigTemplate` | Reusable bet config presets | `TemplateId`, `TemplateName`, `IsDefault` |
| `ConfigTemplateMargin` | Margins within a template | `TemplateId`, `MarketTypeId`, `Margin` |
| `ActivityLog` | Audit trail for all changes | `ClientId`, `UserId`, `Action`, `OldValue`, `NewValue`, `Timestamp` |

## Client lifecycle

```
Pending → Active → Suspended → Inactive
```

- **Pending**: New client, config not yet applied
- **Active**: Live, receiving real-time data
- **Suspended**: Temporarily blocked (trading decision), still in DB
- **Inactive**: Soft-deleted, excluded from all queries

Filter convention — always exclude inactive clients:
```csharp
.Where(c => c.Status != ClientStatus.Inactive)
```

## Bet setting hierarchy

Bet settings resolve in priority order (most specific wins):

```
1. Per-market override (ClientBetSetting where MarketTypeId is specific)
2. Per-league override (ClientBetSetting where LeagueId is specific)
3. Client default (ClientBetSetting where both are null)
4. System default (ConfigTemplate marked IsDefault = true)
```

The resolution logic lives in `BackofficeRepository`. Never replicate it in controllers or services.

## Activity logging — mandatory on all mutations

Every write operation must produce an activity log entry. This is the audit trail for compliance. Use the async RabbitMQ path — do not write directly to the ActivityLog table from the API:

```csharp
await _rabbitMqRepository.Publish(new OnActivityLogMessage
{
    ClientId = clientId,
    UserId = currentUserId,
    Action = "UpdateClientMargin",
    EntityType = "ClientBetSetting",
    EntityId = settingId.ToString(),
    OldValue = JsonSerializer.Serialize(oldSetting),
    NewValue = JsonSerializer.Serialize(newSetting),
    CorrelationId = Guid.NewGuid().ToString().ToUpper()
}, region: "HK");
```

`Backoffice.Utility` consumes `OnActivityLogMessage` and writes to the `ActivityLog` table.

## Config template application

When applying a template to a client:

```
1. Load ConfigTemplate + ConfigTemplateMargins
2. Delete existing ClientBetSettings for this client
3. Insert new ClientBetSettings from template margins
4. Publish OnUpdateClientMarketConfigMessage for each affected market
5. Publish OnActivityLogMessage
6. Invalidate cache keys: "config.margin.{clientId}_*"
```

This is a multi-step operation. Wrap steps 2–3 in `TransactionScope`.

## Region codes

Clients are assigned a `RegionCode` (e.g. `HK`, `EU`, `AS`). Region codes affect:
- RabbitMQ routing — messages are published to region-specific exchanges
- Cache key namespacing — some keys are region-prefixed
- API endpoint visibility — some features are region-gated via config

Always pass `region` when calling `_rabbitMqRepository.Publish`. The correct region comes from `client.RegionCode`.

## User authentication model

Backoffice users are authenticated via JWT. The JWT payload contains:
- `sub` — user ID
- `role` — `Admin`, `Operator`, `ReadOnly`
- `clientId` — if the user is scoped to a specific client (null = superuser)

Controllers read the current user from `HttpContext.User`:
```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var clientId = User.FindFirst("clientId")?.Value;
```

`[Authorize]` enforces JWT presence. Role-based checks use `[Authorize(Roles = "Admin")]` where needed.

## API key usage

`[ApiKey]` (from `ActionFilter/ApiKeyFilter.cs`) is used for internal service-to-service calls. The key is passed in the `x-api-key` header and validated against `appsettings.json`. Never expose API-key-protected endpoints to the browser UI.

## Key cache keys for this domain

```
clients.all                        — full client list
clients.active                     — active clients only
cc:{clientId}                      — individual client config (short form)
config.template.{templateId}       — specific template
config.margin.{clientId}_{marketId}— per-market margin for client
```

When any client mutation occurs, invalidate the relevant keys and broadcast via `OnDeleteCacheMessage`.

## BackofficeRepository scale warning

`BackofficeRepository.cs` exceeds 2000 lines. When adding new methods:
- Add to the correct `#region` block (Public Methods / Private Methods)
- Follow the existing method order (Get → Add → Update → Delete)
- Do not reorganize existing code
