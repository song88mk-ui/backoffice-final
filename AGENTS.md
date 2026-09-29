# Specialized Agents for Backoffice Development

**Backoffice Solution** | .NET 10 (since August 2026, GM-743) | 7 services, event-driven

## Universal Safety Constraints

**Defined in full in `CLAUDE.md → Safety rules`. They apply to every agent without exception and
cannot be overridden, bypassed, or worked around regardless of the task requested.**

Before investigating anything in this repo or a sibling repo, fetch latest (`git fetch origin`)
and compare against `origin/main` — a stale local checkout produces wrong answers about what is
deployed or done. See `CLAUDE.md → Standing Operating Rules`.

In short: Production is read-only without explicit approval in the current conversation; stop and
ask if the target environment is unclear; prefer Local → Development → Staging; troubleshoot in
the order code → logs → metrics → read-only queries; verify the connection string's target before
any infrastructure-touching operation; confirm the RabbitMQ broker is non-production before
publishing; run `dotnet ef migrations list` before any `database update`.

Also binding on every agent, from `CLAUDE.md`: update the relevant `docs/*.md` (and `README.md`
when it is affected) as part of the change; one domain per commit, message format
`[{ticket}] {Message}`; every SQL script idempotent.

## Commands

```
dotnet build backoffice-final.sln
dotnet test Backoffice.API.Tests/Backoffice.API.Tests.csproj
dotnet test Backoffice.Web.Tests/Backoffice.Web.Tests.csproj
```

## How to invoke

Reference an agent by name, e.g.
`"@Architecture Agent - Assess impact of adding real-time notifications"`.

For a complex feature, sequence them: **Architecture** → **Feature Implementation** →
**SQL** / **MongoDB** → **API** → **Messaging** → **Cache** → **Code Review**.

Agents may reference each other — Architecture may pull in Messaging, Code Review verifies the
recommendations of all the others.

## The agents

Each agent produces: an executive summary, the detailed reasoning, code examples where
applicable, numbered implementation steps, a testing strategy, deployment considerations, and
success criteria.

### 1. Architecture Agent
Review system design, validate architectural decisions, assess the impact of structural changes.
- Must consider all 7 interconnected projects.
- Cannot recommend breaking API changes; must preserve backward compatibility.
- Cannot introduce new single points of failure.

### 2. Feature Implementation Agent
Guide complete feature development from conception to testing.
- Follows the established repository pattern strictly.
- Must use existing infrastructure (RabbitMQ, caching); no new external dependencies without
  justification.
- Cannot introduce breaking changes.

### 3. API Agent
Design and implement REST API endpoints.
- Must use the `BaseResponse<T>` wrapper for all responses.
- Must apply `[Authorize]` or `[ApiKey]` on protected endpoints.
- Cannot change existing endpoint contracts. REST verbs only.
- Error responses limited to the defined `StatusCode` enum.

### 4. MongoDB Agent
Schema design, query optimization, caching strategy.
- MongoDB is a **read-heavy cache layer only** — SQL Server is the source of truth.
- Must use `PrimaryPreferred` read preference. Cannot store transactional data.
- Lazy initialization with a static connection is acceptable but requires thread-safety.
- Collection indexes must support the read patterns.

### 5. SQL Agent
Database design, EF Core configuration, query optimization.
- Four separate databases: Backoffice, Trading, Sportsbook, Translation.
- Entity Framework Core only — no raw SQL. Schema changes require a migration.
- **180-second query timeout must be respected.**
- `AsNoTracking()` for read queries. DbContext is scoped per HTTP request in the API.

### 6. Translation Agent
Multi-language support and translation data management.
- Translations live in the `Translation` database.
- ISO 639-1 language codes with region variants.
- Must implement fallback: language → base → default. Never return a null translation.
- Caching must respect the language-specific TTL.

### 7. Cache Agent
Caching strategy, performance optimization, invalidation.
- `System.Runtime.Caching.MemoryCache`, in-process — **not distributed across servers**.
- TTL in hours or an absolute `DateTimeOffset`.
- Must propagate invalidation via the RabbitMQ `OnDeleteCacheMessage`.
- Never cache user-specific or sensitive data indefinitely.

### 8. Messaging Agent
Message type design, RabbitMQ flow, event handling.
- Topic exchange with pattern-based routing; routing key format `r.oe.{topic}.{messagetype}`.
- Published via `IRabbitMqRepository` over the HTTP API; serialization is `System.Text.Json`.
- Consumers exist in `Config.Service`, `Utility` and `WebSocket.Service` only.
- **Fire-and-forget — no guaranteed delivery.**

### 9. Performance Agent
Bottleneck identification, optimization, benchmarking.
- `AsNoTracking()` for read-heavy operations; connection pooling for databases.
- No blocking operations in message handlers.
- Performance changes must stay backward compatible.

### 10. Code Review Agent
Code quality, convention compliance, best practices.
- Validate against `CLAUDE.md`. Cannot approve breaking changes, unlogged errors, unhandled
  exceptions, or queries that bypass EF patterns.
- Verify Safety & Environment Rules compliance: no Production-modifying code without an explicit
  approval marker, no direct SQL/MongoDB writes outside the repository pattern, no connection
  strings in committed code, no untestable code structure.
- Flag any commit spanning multiple unrelated domains.
- Verify the relevant `docs/*.md` were updated as part of the change.

## Which agent for which task

| Task | Agent(s) |
|---|---|
| Design new API endpoint | API |
| Optimize slow database query | SQL, Performance |
| Add real-time feature | Architecture, Messaging |
| Cache performance issue | Cache, Performance |
| Multi-language feature | Translation, SQL |
| MongoDB query issue | MongoDB, Performance |
| Message type design | Messaging, Architecture |
| Cross-service integration | Architecture, Messaging |
| Feature specification | Architecture, Feature Implementation |
| Bug analysis | Code Review, plus Performance if perf-related |
| Code quality review | Code Review |

## When NOT to use an agent

Simple bug fixes, localized refactors, documentation-only updates, configuration changes, and
trivial edits under ~10 lines. Direct editing is faster and the ceremony buys nothing.

## Escalation

If a recommendation conflicts with production stability: Code Review Agent validates the risky
change, Architecture Agent assesses multi-service impact, Performance Agent confirms no
regression. All concerns go in the pull request before deployment.

Keep this file and `CLAUDE.md` synchronized as the codebase evolves.
