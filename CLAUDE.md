# CLAUDE.md — backoffice-final

<!-- song88:standing-rules:begin -->
## Standing rules (every prompt, before anything else)

Shared by every song88 repo. The canonical text is `../song88-claude/standing-rules/STANDING-RULES.md`:
edit it there and run `../song88-claude/standing-rules/sync_standing_rules.py`, never edit this block by hand. A `UserPromptSubmit`
hook in `.claude/settings.json` re-injects these rules on every prompt.

1. **Read this repo's CLAUDE.md and AGENTS.md first.**
2. **Fetch latest before investigating.** Run `git fetch origin` and compare with origin/main for this repo and every sibling repo you read, before citing file contents or repo or deployment state as fact. A local checkout can be hundreds of commits behind. When you cannot or should not pull, read files with `git show origin/main:<path>`, and say so explicitly if you did not check.
3. **Trace in the code, never from memory.** Search the repo and its docs, trace the feature end to end, and cite `file:line`.
4. **Label every claim verified, assumption or unknown.** Never guess.
5. **Explain the plan and wait for approval before any code change.** Then make the smallest change that does the job.
6. **Production is read-only.** Do not run queries, deploy, change schema or invalidate caches unless explicitly instructed.
7. **Update docs and README in the same change** whenever the change affects what they describe.
8. **Every SQL script must be idempotent.** Re-running it is a no-op with the same end state.
9. **Do not modify a sibling repo as a side effect of work here.** Siblings are read-only references unless the user asks for a change in that repo.
10. **Never print, log or commit a secret.** Mask it if it must appear in a log line.
11. **A new project or service ships with tests** in the repo's existing test framework.
12. **Commit, push or open a PR only when explicitly asked.** "Implement it" authorizes local edits only.
13. **Report validation with every change:** build, unit tests, integration tests, manual checks and remaining risks. If something could not be validated, say what and why, name the tests to run before deployment, and mark the work Implementation Complete – Validation Pending, never Completed.
14. **If a workflow step does not apply, say so** instead of skipping it silently.
<!-- song88:standing-rules:end -->

Guidance for Claude Code in **backoffice-final**, the back-office / admin platform of the song88
sportsbook. It is **live production infrastructure processing real financial transactions**. This
file is the quick operating manual; `docs/` is the deep reference (start at `docs/00_INDEX.md`).

## What this repo is

- **.NET 10 | 5 backend services + Blazor Server web + shared library** (7 projects, 2 xUnit test
  projects) | event-driven.
- Brownfield layered services, not VSA/Clean/DDD/Modular Monolith. Do not restructure. Kit skills
  emit greenfield defaults; use the facts below instead.

## Tech stack

Overrides dotnet-claude-kit defaults:

- **.NET 10** (since August 2026, GM-743), all projects `net10.0`. Several dependencies remain on
  legacy majors — RabbitMQ.Client 5.2.0, WindowsAzure.Storage 9.3.3, EPPlus 5.8.2 — and NuGet
  audit flags vulnerable transitives. See `docs/08_TechnicalDebt.md`.
- **Data:** EF Core 10.0.11, four SQL Server DbContexts in `Backoffice.Shared/Data`; MongoDB.Driver
  3.12.0, read-only.
- **Messaging/caching:** raw RabbitMQ.Client 5.2.0; in-process `IMemCache`. No Wolverine,
  MassTransit, outbox, HybridCache or Redis.
- **Tests:** xUnit 2.4.2 (not v3), Moq 4.18.4, EF Core InMemory, bUnit 2.11.3. No Testcontainers,
  WebApplicationFactory, Verify or FluentAssertions. bUnit test classes inherit
  `Backoffice.Web.Tests/BunitTestContext.cs`, not `BunitContext`: it disposes the container
  asynchronously, without which `HubService` (`IAsyncDisposable` only) throws on dispose.
- **Packages:** pinned inline per project file; no central package management.

## Build / test

```
dotnet build backoffice-final.sln
dotnet test Backoffice.API.Tests/Backoffice.API.Tests.csproj
dotnet test Backoffice.Web.Tests/Backoffice.Web.Tests.csproj
```

CI (`.github/workflows/ci.yml`) runs the test job **only on pull requests** — a push to `main`
runs no tests. Restore needs `NUGET_PASSWORD` for the private GitHub feed. Before the tests, the
job builds the solution with `-warnaserror`, so any new compiler warning fails the PR; only the
NuGet vulnerability audit warnings (NU1900-NU1905) stay warnings. Check locally with
`dotnet build backoffice-final.sln -c Release -warnaserror -warnnotaserror:NU1900,NU1901,NU1902,NU1903,NU1904,NU1905`.

## Testing requirements

Standing rule 13 sets the validation report and the "Validation Pending" status. Specific to this repo:

`Backoffice.API.Tests/Translation/TranslationRepositoryTests.cs` has a `BuildSut` overload that
records every `IRabbitMqRepository.Publish` call. Use it to assert what a repository method
publishes (routing key and payload), not only the database state. Every glossary
`OnTranslationChange` delta must set `SrcTx` to the row's SOURCE_TEXT (GM-852). Those tests fail if
a publish site drops it (GM-874).

## The 30-second architecture

- **Shape:** MVC controllers call repositories in `Backoffice.API/Repositories`, hosted via
  `Backoffice.API/Startup.cs`. The only Minimal API endpoint is in `Backoffice.Web/Program.cs`.
- **Auth:** custom filters, not ASP.NET auth middleware: `Backoffice.API/ActionFilter/ApiKeyFilter.cs`
  and `Backoffice.API/ActionFilter/AuthorizeAttribute.cs`.
- **Single-instance WebSocket** — `BackofficeHub` uses a static dictionary, not distributed.
- **No idempotency** — duplicate messages cause duplicate processing.
- **Fire-and-forget publishing** — failures are neither detected nor retried.
- **In-process cache** — not shared across instances.
- **Monolithic repositories** — 2000+ lines, hard to test.

## Critical rules

1. **Production targets.** Any ambiguity about whether a target is Production — stop and ask. "It
   looks like dev" is not confirmation; verify the connection string, hostname and env var
   explicitly. Read-only Production access (logs, metrics, `SELECT`) still needs confirmation.
   Production state changes needing approval for that specific action include: DB writes, schema
   changes, `dotnet ef database update`, cache invalidation, service restarts, k8s/ArgoCD/Helm
   operations, AWS changes, deployments, and publishing to a production exchange.
2. **Availability & integrity gate.** If an action could affect Production availability, data
   integrity or completeness — even indirectly — stop and get confirmation. Triggers: migrating a
   hot table, restarting a service mid-message-processing, publishing an `OnDeleteCacheMessage` on
   `r.oe.cachesync.*` (it clears caches in downstream services; `IMemCache` itself is in-process),
   publishing a message that cascades to the OddsEngine.
3. **RabbitMQ isolation.** Messages on the OddsEngine exchange (`r.oe.*`) are indistinguishable from
   production events once they leave the broker — local code against a production broker triggers
   real config changes on the live OddsEngine. Confirm `RabbitMQ:Host` is non-production before
   running anything that publishes. Use Staging, with confirmation, if a real publish is required.
4. **No GUI writes to Production.** SSMS, Azure Data Studio and MongoDB Compass bypass
   application-layer validation, activity logging and cache invalidation. Production data changes
   go through the API layer. Even read-only GUI access to Production needs confirmation.
5. **Schema changes.** This repo has no EF Core migrations (no `Migrations` folder and no
   `Microsoft.EntityFrameworkCore.Design`/`Tools` package); schema is managed by flyway scripts in
   `../infrastructure/flyway`. Confirm the target host before running any script, never run one
   that drops columns or changes types without a reviewed rollback plan, and get explicit written
   approval in the current conversation before any production schema change.
6. **`Backoffice.Shared` is referenced by all 6 other projects (5 services + Web).** Any change to
   its models, message classes or DbContext definitions is a cross-service breaking change if
   deployed to one service alone. Document deployment order and rollback sequence before shipping
   a Shared change to Production.
7. **Secrets.** Committed `appsettings.json` files hold `${...}` placeholders since DEVOPS-371; the
   real values they used to contain are still in git history and pending rotation
   (`docs/08_TechnicalDebt.md` #2). Deployed values come from argocd; local values go in env vars,
   never in a committed file. Before any operation touching external infrastructure, verify which
   environment the active connection string targets. A sibling repo (platform-final, GM-880,
   2026-09-30) found `_logger.LogError` printing an OG private API key on every failed bet
   placement, visible in PRD Loki; mask a secret (keep only the last few characters, blank the
   rest) before it reaches a log line, rather than passing the raw value as a template argument.
8. **Never put quotes around `FromSql` interpolations.** `FromSql($"Proc '{x}'")` turns `{x}` into
   a SQL parameter, so the quotes send the literal text `'@p0'` instead of the value. Write
   `FromSql($"Proc {x}")`. Confirmed via direct `EXEC` against Staging (BO-113, 2026-10-02): the
   quoted form throws `Msg 241: Conversion failed when converting date and/or time from character
   string` on every single call, since the stored proc's own `CAST(@From AS datetime2)` chokes on
   the literal text `'@p2'`. The repository's catch-all swallows this into `StatusCode.Error`, and
   every "View Logs" modal in Backoffice.Web then shows it identically to a genuine empty result
   ("No Record Found!"), so the bug reads as "logs are empty" rather than "logs are broken."
   All five known offenders are fixed as of BO-113 (2026-10-02): in
   `Backoffice.API/Repositories/BackOfficeRepository.cs`, `GetPriceboostActivityLogs`,
   `GetManualBetSettlementHistoryLogs` and the generic `GetActivityLogs(ActivityLogRequest)` (shared
   by every "View Logs" modal in Backoffice.Web: Client Site Config, OddsEngine templates/market
   groups/boosted settings/sport settings, user groups, message settlement, cancel bet history);
   in `Backoffice.Utility/Repository/BackofficeRepository.cs`, `GetActivityLogs` and
   `GetManualBetSettlementActivityLogs`. The `GetActivityLogs` stored procedure is also confirmed
   missing entirely in DEV (not only PRD as previously noted here) - calling it there throws
   instead of returning empty, caught by the same catch-all (GM-134, BO-113).
9. **bo-web DataProtection keys only survive a restart if `DataProtection:KeysPath` is set.**
   `AddDataProtectionKeys()` (`Backoffice.Web/Extensions/WebApplicationBuilderExtensions.cs`) fixes the
   application name to `bo-web` and persists keys to `DataProtection:KeysPath`. Without that path (or
   with a path that is not shared, durable storage) every pod has its own key ring, and the logs show
   `The key {...} was not found in the key ring` (`ErrorUnprotectingCookie`) and antiforgery decrypt
   errors after a restart (BO-114). These errors come from the key ring, not from the session code.

## How this team writes code

- **Focused commits — one domain per commit.** Mixed-purpose commits make bugs harder to isolate
  and rollbacks harder to scope. Split multi-domain work into an ordered commit sequence.
  **Mandatory message format:** `[{ticket}] {Message}` — e.g.
  `[BO-76] Fixed sub user auto disabling when editing sub user`. The ticket is usually encoded in
  the branch name. If none exists, ask for one rather than committing without it.
- **Which doc to update.** New message type → `docs/06_Messaging.md`; new project or major
  component → `docs/02_ProjectOverview.md`; debt introduced or resolved →
  `docs/08_TechnicalDebt.md`. PR descriptions state what changed, which services are affected,
  and what needs testing.
- **SQL scripts.** Guard `INSERT` with `NOT EXISTS`/`MERGE`, schema changes with
  `IF NOT EXISTS`/`OBJECT_ID(...) IS NULL`, and write `UPDATE`/`DELETE` as target-state rather than
  incremental. Say so in the script header. Applies to the sibling repos too.
- **Detailed conventions live in `docs/`**, read on demand. Repository, service, MongoDB, SQL,
  cache, translation, API and logging conventions were inlined here until 2026-09-21. The database
  ones are in `docs/05_DatabaseFlow.md`, messaging in `docs/06_Messaging.md`, caching in
  `docs/07_Caching.md`, and the rest in `docs/01_Architecture.md`.
- **Logging rules** (shared with odds-engine-2):
  - Pass the exception as the first argument: `_logger.LogError(ex, "... {Payload}", payload)`.
    Never put `ex.Message` or `ex.StackTrace` in the message text, and never pass the exception
    as a template argument.
  - Use PascalCase placeholders (`{Message}`, `{Payload}`).
  - Keep operational log lines (start, connecting, connected, progress). Do not remove or merge
    them to satisfy SonarQube S6664.
  - Many existing calls pass `ex` first but keep `{ex.Message}` in their text, left unchanged when
    the exception argument was added (`docs/01_Architecture.md:232-236`). Do not copy that into new calls.

## Where things live

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

## External systems

**Observability**

- **Grafana:** https://grafana.stg.song88.com/ and https://grafana.prd.song88.com/ — Google OAuth
  with an `@song88.com` account. Loki for logs, Prometheus for metrics.
- **Loki labels:** bo-api and bo-web run in namespace `corp-apps` as `app="bo-api"` /
  `app="bo-web"`. The STG cluster also hosts the `dev` namespace. Log lines are Serilog JSON:
  `@l` (level), `@mt` (template), `@x` (exception), `RequestPath`, and `commandText` for EF SQL.
- The OAuth browser flow does not work for API calls; generate a service account token from
  Grafana Administration for REST access.

**BO API access (local / ad-hoc debugging)**

Staging or DEV only unless Production is explicitly authorised in the current conversation.
Most endpoints need **two** headers: `x-api-key` (`Security:ApiKey`) **and**
`Authorization: Bearer <JWT>`. Get the JWT from
`POST {BO_API_BASE}/api/authentication/login`. Credentials live in a git-ignored
`.env.bo-api.local` at the repo root, if present. **DEV:** the same file works against DEV
by overriding `BO_API_BASE=https://bo-api.dev.song88.com` (dev `bo-api` uses the staging Auth
API and API key); do not create a separate DEV env file. DEV SQL Server and MongoDB are reached
directly with `sqlcmd` / `mongosh`, allow-listed in the git-ignored `.claude/settings.local.json`.
Full procedure: `docs/16_BoApiAccess.md`.

## Development workflow

1. Investigate freely: reading code and read-only queries need no approval.
2. Before any code change — bug fixes, new tests and refactors included — the plan states the
   files to be touched, the exact change, the tests to be added and the validation steps.
3. The plan also answers: Multiple projects? Migration needed? Other services consuming the
   message? Cache-invalidation implications? Breaks an API contract or backward compatibility?
   Performance implications?
4. After approval, implement, validate per **Testing requirements**, update the matching `docs/`
   file, and summarize the changes, risks and validation performed.

## Engineering principles

- **Prefer the lowest environment necessary:** Local → Development → Staging → Production.
- **Troubleshoot in this order** before proposing any operational change: code analysis → logs →
  metrics → read-only queries (`AsNoTracking()`, read replica if available).
- **Testability.** Keep logic out of constructors and static initializers; accept injected
  dependencies rather than `new`-ing inside methods. Document intentionally untestable code (e.g.
  legacy static MongoDB init) in `docs/08_TechnicalDebt.md`.

## Scope & build reminders

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

## Assets in this repo

- **`docs/`** — the knowledge base; `docs/00_INDEX.md` lists everything.
- **`.claude/README.md`** — index of commands and skills, and the agent catalogue.
- **`.claude/commands/`** — analyze, api, bug, cache, compare, implement, mongodb, performance,
  push-prd, push-stg, refactor, review, sql, translation.
- **`.claude/skills/`** — 16 skills: domain (backoffice, oddsengine, sportsbook), workflow
  (feature-implementation, bug-fixing-workflow, code-review-checklist), technical (api-development,
  caching, messaging, mongodb-best-practices, sql-best-practices, repository-pattern,
  performance-optimization, translation-system, enterprise-architecture) and jira-updates.

## See also

[AGENTS.md](AGENTS.md) · `.claude/README.md` · [README.md](README.md) ·
`docs/00_INDEX.md` · the workspace root `../CLAUDE.md` and `../INTEGRATION-MAP.md`, which describe
the cross-repo couplings (the shared `e.central` exchange, shared databases, the HTTP mesh).
