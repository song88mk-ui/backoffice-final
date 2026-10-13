# .claude/ — backoffice-final

This directory holds the Claude Code assets for backoffice-final: `settings.json` (permissions and
the standing-rules hook), `commands/` (slash commands) and `skills/`. Read [../CLAUDE.md](../CLAUDE.md)
and [../AGENTS.md](../AGENTS.md) first; this file indexes the commands and skills, then describes the agent roles.

## commands/
/analyze · /api · /bug · /cache · /compare · /implement · /mongodb · /performance · /push-prd
· /push-stg · /refactor · /review · /sql · /translation

## skills/
api-development · backoffice-domain · bug-fixing-workflow · caching · code-review-checklist
· enterprise-architecture · feature-implementation · jira-updates · messaging · mongodb-best-practices
· oddsengine-domain · performance-optimization · repository-pattern · sportsbook-domain
· sql-best-practices · translation-system

## Rules and synced files

Workflow and rules: [../CLAUDE.md](../CLAUDE.md) (standing rules, § Development workflow, § Engineering principles).

The `jira-updates` skill and [`.github/pull_request_template.md`](../.github/pull_request_template.md) are
generated from `song88-claude` by `sync_standing_rules.py`. Do not edit them here; change them in
`song88-claude`.

## Agent catalogue

Ten review roles, each a focus area rather than a separate tool. Invoke one by name in a prompt, e.g.
`"@Architecture Agent - assess the impact of adding real-time notifications"`. For a complex
feature, sequence them: Architecture → Feature Implementation → SQL / MongoDB → API → Messaging →
Cache → Code Review. Each answer gives a summary, the reasoning, code examples where useful,
numbered implementation steps, a testing strategy, deployment considerations and success criteria.

### 1. Architecture
System design, structural changes and their impact.
- Consider all 7 projects: 5 backend services (`Backoffice.API`, `Backoffice.WebSocket.API`,
  `Backoffice.Config.Service`, `Backoffice.Utility`, `Backoffice.WebSocket.Service`), the
  `Backoffice.Web` UI and `Backoffice.Shared`.
- Preserve backward compatibility of API contracts and message classes; `Backoffice.Web` and
  sibling repos consume them.
- Do not add a new single point of failure. The SignalR hub is already single-instance
  (CLAUDE.md § The 30-second architecture).

### 2. Feature implementation
A feature from design to tests.
- Follow the existing shape: controller → repository in `Backoffice.API/Repositories`, returning
  `BaseResponse<T>`.
- Reuse the existing infrastructure (RabbitMQ, `IMemCache`); justify any new external dependency.

### 3. API
REST endpoints in `Backoffice.API`.
- Repositories return `BaseResponse<T>` (`Backoffice.Shared/Models/BaseResponse.cs`) and
  controllers wrap it in `Ok(...)` / `BadRequest(...)`. Its `StatusCode` is the
  `Backoffice.Shared.Enums.StatusCode` enum (`Backoffice.Shared/Enums/StatusCode.cs`); add a value
  there rather than inventing codes.
- Auth is two custom filters, not ASP.NET middleware: `ServiceFilter(typeof(ApiKeyFilter))` checks
  `x-api-key` against `Security:ApiKey`, and the custom `[Authorize]` / `[Authorize("<permission>",
  (int)Access.View|Edit)]` in `Backoffice.API/ActionFilter/AuthorizeAttribute.cs` checks the JWT.
  Every controller except `AuthenticationController` has the API-key filter and `[Authorize]` at
  class or action level; `[AllowAnonymous]` (the ActionFilter one) opts an action out of the JWT
  check. There is no global fallback, so a new controller needs both.
- Do not change an existing endpoint contract. Endpoints are `HttpGet`/`HttpPost` plus one
  `HttpDelete`; JSON is Newtonsoft (`AddNewtonsoftJson`, `Backoffice.API/Startup.cs:126`).

### 4. MongoDB
- BO only reads MongoDB (`Custom:MongoDB-Sportsbook`, database `Sportsbook`); SQL Server is the
  source of truth and there are no Mongo write calls in this repo.
- Read preference is set per collection: `PrimaryPreferred` for the lookup collections
  (`SportsbookRepository.cs:60-63`), `Secondary` for Fixture/Market/Selection
  (`TradingRepository.cs:52-54`). Match the existing choice for the collection you read.
- The client is a static field initialised lazily in the repository constructor
  (`if (_db == null)`, no lock). Do not add state that depends on it being created once.

### 5. SQL
- Four SQL Server databases, one DbContext each in `Backoffice.Shared/Data`: Backoffice,
  Sportsbook, Trading, Translation.
- No EF Core migrations; schema is flyway in `../infrastructure/flyway` (CLAUDE.md § Critical
  rules, rule 5).
- Prefer EF LINQ. Raw SQL exists, always parameterized: `ExecuteSqlRawAsync` with `{0}` placeholders
  or `SqlParameter` in `Backoffice.API/Repositories/ClientImportRepository.cs`,
  `SportsbookRepository.cs` and `BackOfficeRepository.cs` (`FromSqlRaw`), plus an ADO.NET
  `SqlCommand` in `ClientImportRepository.RegisterTradingClient`. Never concatenate values into
  SQL text, and never quote a `FromSql` interpolation (CLAUDE.md § Critical rules, rule 8).
- The API's command timeout is `Custom:DbContextTimeout`, default 180 s
  (`Backoffice.API/Startup.cs:91`). The worker services build `BackofficeContext` by hand without
  it, and the ADO.NET command uses the 30 s default.
- Use `AsNoTracking()` for reads. In the API, DbContexts are scoped per request (Backoffice and
  Translation are pooled, `Startup.cs:98-102`).

### 6. Translation
- UI and sportsbook translations live in the `Translation` database (`TranslationContext`:
  `BackofficeStatic`, `SportsbookStatic`).
- Language codes are the upper-case `LanguageCode` enum (`Backoffice.Shared/Enums/LanguageCode.cs`):
  mostly ISO 639-1, but `BR` means Brazilian Portuguese. No region variants.
- `GetTranslationsByLanguageCode` (`BackOfficeRepository.cs:2651`) overlays the requested language
  on `EN`; a key missing from both is absent, not defaulted. Result cached per language for 24 h.
- Glossary changes publish `OnTranslationChange` with `SrcTx` set (CLAUDE.md § Testing requirements).

### 7. Cache
- `IMemCache` (`Backoffice.Shared/Utilities/MemCache.cs`) wraps `System.Runtime.Caching.MemoryCache`:
  in-process, not shared between instances. TTL is an int in hours or an absolute `DateTimeOffset`.
- Nothing in BO evicts its own `IMemCache` entries on a message; they expire. `OnDeleteCacheMessage`
  on `r.oe.cachesync.*` clears caches in downstream services (CLAUDE.md § Critical rules, rule 2).
- Do not cache user-specific or sensitive data for long.

### 8. Messaging
- One topic exchange; routing keys are `r.<area>.<...>`: `r.oe.cachesync.*`, `r.oe.web.config.*`,
  `r.uof.*` outbound, `r.bo.config.#` and `r.bo.util.#` inbound. See `docs/06_Messaging.md`.
- `Backoffice.API` publishes through `IRabbitMqRepository`, an HTTP POST to `v1/broker/publish`
  (`API:RabbitMqApi:{HK|SA}:Uri`). `Backoffice.Config.Service` and `Backoffice.Utility` publish
  directly over AMQP with `MessageBroker.Publish`. Payloads are `System.Text.Json`.
- Consumers (`MessageBroker.Connect` + `MessageReceived`): `Backoffice.Config.Service`
  (`r.bo.config.#`), `Backoffice.Utility` (`r.bo.util.#`) and `Backoffice.WebSocket.Service`
  (odds change, bet stop, notifications). `Backoffice.Web/Middleware/RabbitMQMiddleware.cs` also
  subscribes but is not registered.
- Fire-and-forget: a failed publish is neither detected nor retried, and there is no idempotency
  (CLAUDE.md § The 30-second architecture).

### 9. Performance
- `AsNoTracking()` for read-heavy queries.
- No blocking work in message handlers. `Backoffice.Config.Service/Worker.cs` handles messages
  synchronously with sync EF calls, so a slow message stalls that queue.
- Performance changes stay backward compatible.

### 10. Code review
- Check the change against CLAUDE.md § Critical rules and § How this team writes code.
- Reject breaking changes, swallowed or unlogged exceptions, unparameterized SQL, and SQL or Mongo
  access outside a repository.
- Flag a commit spanning unrelated domains.

## Which agent for which task

| Task | Agent(s) |
|---|---|
| New API endpoint | API |
| Slow database query | SQL, Performance |
| Real-time feature | Architecture, Messaging |
| Cache issue | Cache, Performance |
| Multi-language feature | Translation, SQL |
| MongoDB query issue | MongoDB, Performance |
| New message type | Messaging, Architecture |
| Cross-service integration | Architecture, Messaging |
| Feature specification | Architecture, Feature implementation |
| Bug analysis | Code review, plus Performance if perf-related |
| Code quality review | Code review |

## When not to use an agent

Simple bug fixes, localized refactors, documentation-only updates, configuration changes and edits
under about 10 lines. Direct editing is faster.

## Escalation

If a recommendation could affect production stability: Code review checks the risky change,
Architecture assesses multi-service impact, Performance confirms no regression. Record the concerns
in the pull request before deployment.
