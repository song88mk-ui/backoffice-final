## ⚠️ Standing Operating Rules (read first, every prompt)

> **These are the first instruction. They apply to every task and override convenience or time pressure. A `UserPromptSubmit` hook re-injects them on every prompt.**

1. **Consult CLAUDE.md and AGENTS.md first.** Follow their mandatory workflow and the Safety & Environment Rules exactly.
2. **Trace claims in the actual code and cite `file:line`** — do not answer from memory.
3. **Label every claim** as `[verified]` (confirmed in code/output this session), `[assumption]` (inferred, not checked), or `[unknown]` (not determinable). Do not guess.
4. **Production is READ-ONLY.** Do not run queries, deploy, change schema, publish messages, or invalidate caches against Production unless the user explicitly says so in this conversation. Prefer the lowest environment necessary (Local → Development → Staging → Production).
5. **Explain the plan and wait for approval before making any code change** — bug fixes, new tests, and refactors included. Investigation (reading code, read-only queries) may proceed without approval. The plan must state: files to be touched, the exact change, tests to be added, validation steps. Make the smallest possible change.
6. **If architecture changed,** update `docs/*.md` + CLAUDE.md/AGENTS.md and summarize the changes, risks, and validation performed.

---

# Backoffice

.NET 10 | 5 backend services + Blazor Server web + shared library (7 projects, 2 xUnit test projects) | event-driven | **live production infrastructure processing real financial transactions**

## Commands

```
dotnet build backoffice-final.sln
dotnet test Backoffice.API.Tests/Backoffice.API.Tests.csproj
dotnet test Backoffice.Web.Tests/Backoffice.Web.Tests.csproj
```

CI (`.github/workflows/ci.yml`) runs the test job **only on pull requests** — a push to `main`
runs no tests. Restore needs `NUGET_PASSWORD` for the private GitHub feed.

## Safety rules — permanent, not overridable

**Production protection.** Never connect to, query, or interact with Production unless explicitly
instructed in the current conversation. Any ambiguity about whether a target is Production — stop
and ask. "It looks like dev" is not confirmation; verify the connection string, hostname and env
var explicitly.

Read-only Production access (logs, metrics, `SELECT`) still needs confirmation. **Never** modify
Production state without approval for that specific action: DB writes, schema changes,
`dotnet ef database update`, cache invalidation, service restarts, k8s/ArgoCD/Helm operations,
AWS changes, deployments, or publishing to a production exchange.

**Troubleshoot in this order** before proposing any operational change: code analysis → logs →
metrics → read-only queries (`AsNoTracking()`, read replica if available).

**Availability & integrity gate.** If an action could affect Production availability, data
integrity or completeness — even indirectly — stop and get confirmation. Triggers: migrating a
hot table, restarting a service mid-message-processing, publishing an `OnDeleteCacheMessage` on
`r.oe.cachesync.*` (it clears caches in downstream services; `IMemCache` itself is in-process), publishing a message that cascades to the OddsEngine.

**Secrets.** Never commit real connection strings, API keys, JWT secrets or RabbitMQ credentials.
Committed settings files (notably `Backoffice.API/appsettings.json`) currently contain real
credentials, a known leak pending rotation; do not add more. Real values go in git-ignored
Development overlays, env vars or a secrets manager. Before any operation touching
external infrastructure, verify which environment the active connection string targets.

**RabbitMQ isolation.** Messages on the OddsEngine exchange (`r.oe.*`) are indistinguishable from
production events once they leave the broker — local code against a production broker triggers
real config changes on the live OddsEngine. Confirm `RabbitMQ:Host` is non-production before
running anything that publishes. Use Staging, with confirmation, if a real publish is required.

**Schema changes.** This repo has no EF Core migrations (no `Migrations` folder and no
`Microsoft.EntityFrameworkCore.Design`/`Tools` package); schema is managed by flyway scripts in
`../infrastructure/flyway`. Confirm the target host before running any script, never run one that
drops columns or changes types without a reviewed rollback plan, and get explicit written approval
in the current conversation before any production schema change.

**`Backoffice.Shared` is referenced by all 6 other projects (5 services + Web).** Any change to its models, message classes
or DbContext definitions is a cross-service breaking change if deployed to one service alone.
Document deployment order and rollback sequence before shipping a Shared change to Production.

**No GUI writes to Production.** SSMS, Azure Data Studio and MongoDB Compass bypass
application-layer validation, activity logging and cache invalidation. Production data changes go
through the API layer. Even read-only GUI access to Production needs confirmation.

## Change rules

**Documentation.** Every code change updates the relevant `docs/*.md`: new message type →
`docs/06_Messaging.md`; new project or major component → `docs/02_ProjectOverview.md`; debt
introduced or resolved → `docs/08_TechnicalDebt.md`. Update `README.md` in the same change
whenever it affects anything the README documents. PR descriptions state what changed, which
services are affected, and what needs testing.

**Focused commits — one domain per commit.** Mixed-purpose commits make bugs harder to isolate and
rollbacks harder to scope. Split multi-domain work into an ordered commit sequence.
**Mandatory message format:** `[{ticket}] {Message}` — e.g.
`[BO-76] Fixed sub user auto disabling when editing sub user`. The ticket is usually encoded in the branch name. If none
exists, ask for one rather than committing without it.

**SQL idempotency.** Every SQL script (data fixes, seeds, hand-run or flyway migrations) must
produce the same end state on re-run. Guard `INSERT` with `NOT EXISTS`/`MERGE`, schema changes
with `IF NOT EXISTS`/`OBJECT_ID(...) IS NULL`, and write `UPDATE`/`DELETE` as target-state rather
than incremental. Say so in the script header. Applies to the sibling repos too.

**Testability.** Keep logic out of constructors and static initializers; accept injected
dependencies rather than `new`-ing inside methods. Document intentionally untestable code (e.g.
legacy static MongoDB init) in `docs/08_TechnicalDebt.md`.

## Testing requirements (mandatory)

Code is not "complete" until validation is performed, or its absence explicitly documented.
Every implementation summary must report: **Build Status**, **Unit Tests**, **Integration Tests**,
**Manual Verification**, **Remaining Risks**.

If testing is not possible: state what could not be validated and why, recommend the exact tests
to run before deployment, and mark the work **Implementation Complete – Validation Pending**,
never **Completed**.

## Critical constraints

1. **.NET 10** (since August 2026, GM-743), all projects `net10.0`. Several dependencies remain on
   legacy majors — RabbitMQ.Client 5.2.0, WindowsAzure.Storage 9.3.3, EPPlus 5.8.2 — and NuGet
   audit flags vulnerable transitives. See `docs/08_TechnicalDebt.md`.
2. **Single-instance WebSocket** — `BackofficeHub` uses a static dictionary, not distributed.
3. **No idempotency** — duplicate messages cause duplicate processing.
4. **Fire-and-forget publishing** — failures are neither detected nor retried.
5. **In-process cache** — not shared across instances.
6. **Monolithic repositories** — 2000+ lines, hard to test.

## Before modifying code, ask

Multiple projects? Migration needed? Other services consuming the message? Cache-invalidation
implications? Breaks an API contract or backward compatibility? Performance implications?

## Conventions — see docs/

Detailed conventions live in `docs/` and are read on demand, not inlined here:

| Topic | Doc |
|---|---|
| Architecture, system design | `docs/01_Architecture.md` |
| All 7 projects described | `docs/02_ProjectOverview.md` |
| Project dependency graph | `docs/03_DependencyGraph.md` |
| Startup and DI wiring | `docs/04_StartupFlow.md` |
| **MongoDB and SQL conventions** — read preference, contexts, query patterns | `docs/05_DatabaseFlow.md` |
| Messaging, RabbitMQ flows, message types | `docs/06_Messaging.md` |
| Caching strategy and invalidation | `docs/07_Caching.md` |
| Technical debt | `docs/08_TechnicalDebt.md` |
| Authentication | `docs/09_Authentication.md` |
| Code smells, SOLID violations | `docs/11_CodeSmells.md` |
| Known bugs | `docs/10_Bugs.md` |
| Performance bottlenecks | `docs/12_PerformanceBottlenecks.md` |
| Concurrency and thread-safety risks | `docs/13_RiskyImplementations.md` |
| Settlement flows | `docs/14_SettlementFlows.md` |
| Sibling repositories | `docs/15_ReferenceRepositories.md` |
| BO API access, curl recipe | `docs/16_BoApiAccess.md` |

Repository, service, MongoDB, SQL, cache, translation, API and logging conventions were inlined
here until 2026-09-21. The database ones are in `docs/05_DatabaseFlow.md`, messaging in
`docs/06_Messaging.md`, caching in `docs/07_Caching.md`, and the rest in
`docs/01_Architecture.md`. `docs/00_INDEX.md` lists everything.

## Sibling repositories

Siblings of `backoffice-final` in the same parent. Use relative paths, never absolute.

| Repo | Contents |
|---|---|
| `../Auth/` | Auth.Api — JWT issuance; this repo validates its tokens |
| `../odds-engine-2/` | OddsEngine v2: OE.Adapter.Service, OE.Sportsbook.Service, OE.UOF.Client, OE.Job.Settlement |
| `../platform-final/` | Platform.Settlement.Service, Platform.Operator.Servicev2, Platform.Sportsbook.API, Platform.CashOutStake.Servicev2, Platform.Shared |
| `../platform-web/` | Platform web frontend (Blazor) |
| `../bet-genius/` | BetGenius integration |
| `../webprops-api/` | Web Properties API (Go) — per-client flags/themes/layouts + maintenance, S3-backed. Consumed via `IWebPropsRepository` ("Client Site Config" page, renamed from "Feature Flags", GM-696; the webprops key is the client's Platform GUID) and `MaintenanceRepository` (`MaintenanceV2`, `GET/POST /api/v1/maintenance` with `X-Layout`; replaced the dead `bo-maintenance-api` Dapr router, BO-2 2026-08) |
| `../argocd/` | Kubernetes / ArgoCD deployment configs |
| `../infrastructure/` | helmfile, terraform, flyway migrations |

The workspace root `../CLAUDE.md` and `../INTEGRATION-MAP.md` describe the cross-repo couplings
(the shared `e.central` exchange, shared databases, the HTTP mesh).

## Observability

- **Grafana (staging):** https://grafana.stg.song88.com/ — Google OAuth with an `@song88.com`
  account. Loki for logs, Prometheus for metrics.
- The OAuth browser flow does not work for API calls; generate a service account token from
  Grafana Administration for REST access.

## BO API access (local / ad-hoc debugging)

Staging only unless Production is explicitly authorised in the current conversation.
Most endpoints need **two** headers: `x-api-key` (`Security:ApiKey`) **and**
`Authorization: Bearer <JWT>`. Get the JWT from
`POST {BO_API_BASE}/api/authentication/login`. Credentials live in a git-ignored
`.env.bo-api.local` at the repo root, if present. Full procedure: `docs/16_BoApiAccess.md`.
