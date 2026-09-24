---
name: enterprise-architecture
description: Topology of the Backoffice services: which project owns, consumes and publishes what, the request lifecycle with async side effects, cross-service contract rules and deployment order for Shared changes. Use when making cross-cutting decisions, adding a project, or tracing a flow across service boundaries.
---

# Enterprise Architecture

Domain knowledge for the Backoffice distributed platform architecture. Apply this when making cross-cutting decisions, adding new projects, or tracing request flows across service boundaries.

---

## System topology

```
Browser / External Client
        │
        ▼
Backoffice.Web  (Blazor Server, port varies)
        │  HTTP (HttpClient to API)
        ▼
Backoffice.API  (ASP.NET Core, primary gateway)
        │
   ┌────┼────────────────┐
   │    │                │
   ▼    ▼                ▼
SQL DB  MongoDB     RabbitMQ exchange
        (Sportsbook     │
         cache)    ┌────┼──────────────┐
                   ▼    ▼              ▼
          Config.Service  Utility  WebSocket.Service
                                        │
                                        ▼
                              Backoffice.WebSocket.API
                              (SignalR hub, real-time)
```

## Project ownership matrix

| Project | Owns | Consumes | Publishes |
|---------|------|----------|-----------|
| Backoffice.API | REST endpoints, business logic, SQL writes | SQL, MongoDB, RabbitMQ | RabbitMQ messages |
| Backoffice.Web | Blazor UI, user sessions | Backoffice.API (HTTP) | Nothing |
| Backoffice.WebSocket.API | SignalR hub, real-time state | SignalR connection from Backoffice.WebSocket.Service (no RabbitMQ consumer) | SignalR events to browsers |
| Backoffice.Config.Service | Config change processing: `UpdateClientMargin`, `DeleteConfigTemplate` on `r.bo.config.*` (`Config.Service/Worker.cs:58,62`) | RabbitMQ, SQL | `OnUpdateClientMarketConfig`, `OnUpdateClientLeagueConfig` on `r.oe.web.config.*` (`Config.Service/Repository/BackofficeRepository.cs:90,118`) |
| Backoffice.Utility | Activity logging, exports (`Utility/Worker.cs:81-105`) | RabbitMQ, SQL, MongoDB | Nothing |
| Backoffice.WebSocket.Service | Message routing to SignalR (`Repositories/SportsbookRepository.cs`) | RabbitMQ | SignalR hub calls to Backoffice.WebSocket.API |
| Backoffice.Shared | Shared models, DbContexts, utilities | — | — |

## Shared library rules

`Backoffice.Shared` is referenced by all projects. Any change to it affects every service:
- Adding a property to a model — safe (additive)
- Removing or renaming a model property — **breaking**, coordinate all consumers
- Adding a new `OutgoingEngineMessages` class — safe
- Changing an existing message class field — **breaking**, old consumers silently ignore unknown fields (JSON) but missing required fields cause deserialization failures

## Request lifecycle (API write with async side-effects)

```
1. HTTP POST → Backoffice.API controller
2. Controller → IXxxRepository.AddXxxAsync(request)
3. Repository validates, writes to SQL (BackofficeContext)
4. Repository writes the activity log directly to SQL → `IBackofficeActivityLogRepository.ProcessActivityLog` (`BackofficeActivityLogRepository.cs:19-43`), not over RabbitMQ
5. Repository asks downstream services to drop caches → OnDeleteCacheMessage → `r.oe.cachesync.*` (no Backoffice consumer)
6. Repository publishes a config command (e.g. `UpdateClientMargin` on `r.bo.config.updateclientmargin`, `BackOfficeRepository.cs:2106`) → RabbitMQ
7. Backoffice.Config.Service receives the config command → processes SQL changes, publishes `r.oe.web.config.*`
8. (Backoffice.Utility has `LogActivity`/`LogActivityList` handlers, `Utility/Worker.cs:81,85`, but no publisher for them was found in the workspace)
9. Backoffice.WebSocket.Service receives message → notifies connected clients via SignalR
10. HTTP 200 returned to caller with BaseResponse<T>
```

Steps 5–9 are fire-and-forget. The HTTP response is sent before background processing completes.

## Cross-service contract rules

1. **Never remove** a message type that any consumer handles — the consumer will silently skip an unrecognised type, but removing a handled type leaves dead `case` blocks and may break expected behaviour.
2. **Never remove** an API endpoint that `Backoffice.Web` calls — trace all `HttpClient` calls in `Backoffice.Web/Services/` before deprecating anything.
3. **Never change** a `BaseResponse<T>` field name — all Blazor services deserialize this directly.
4. **Routing keys** follow `r.{oe|bo|uof}.{topic}.{messagetype}`, mostly lowercase (e.g. `r.oe.cachesync.onclientchange`, `r.bo.config.updateclientmargin`, `r.uof.onbetsettlement`); two Config.Service keys are mixed case (`r.oe.web.config.OnUpdateClientMarketConfig`, `r.oe.web.config.OnUpdateClientLeagueConfig`). Match the existing key exactly. Changing a routing key orphans existing consumers.

## Deployment order for breaking changes

If a Shared model must change:
1. Deploy `Backoffice.Shared` update with the **old + new fields both present** (additive)
2. Deploy all consumer services with updated deserialization
3. Deploy producer with updated publishing
4. (Later) Remove the deprecated field

## Known architectural constraints

| Constraint | Location | Impact |
|-----------|----------|--------|
| Static MongoDB `_db` not thread-safe | `SportsbookRepository.cs:29`, `TradingRepository.cs:28` | Possible race on first init |
| `BackofficeHub` uses static Dictionary | `Backoffice.WebSocket.API/Hubs/` | Not horizontally scalable |
| IMemCache is in-process only | All projects | Each instance has its own cache; invalidation via RabbitMQ is mandatory |
| Fire-and-forget RabbitMQ publish | All repositories | Publish failures are undetected |
| Legacy dependency majors on .NET 10 | RabbitMQ.Client 5.2.0, WindowsAzure.Storage 9.3.3, EPPlus 5.8.2 | Library upgrade constraints; see docs/08_TechnicalDebt.md |
| No idempotency on message consumers | Worker.cs files | Duplicate messages cause duplicate processing |
