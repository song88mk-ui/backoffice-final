# BackOffice

Next Generation BackOffice Application - the back-office / admin platform for the
sportsbook. Operators use it to manage clients and their configuration, trading
mappings and settlements, translations, feature flags and maintenance mode. It is a
distributed, event-driven .NET solution: a Blazor Server UI, a REST API gateway, a
SignalR hub and three background workers, all sharing the `Backoffice.Shared` class
library.

> Before working in this repo, read `CLAUDE.md` and `AGENTS.md` - they contain
> mandatory workflow, safety and environment rules (production is read-only,
> RabbitMQ isolation, commit format, etc.). Deep-dive docs live in `docs/`
> (start at `docs/00_INDEX.md`).

## Projects

| Project | Type | Target framework | Purpose |
|---|---|---|---|
| `Backoffice.API` | ASP.NET Core Web API | net10.0 | Primary REST gateway; controllers, repositories, SQL + Mongo access, Swagger |
| `Backoffice.Web` | Blazor Server | net10.0 | The back-office UI; talks to Backoffice.API over HTTP and to the WebSocket hub over SignalR |
| `Backoffice.WebSocket.API` | ASP.NET Core Web API | net10.0 | SignalR hub (`/bohub`, MessagePack) pushing real-time updates to Web clients |
| `Backoffice.WebSocket.Service` | Worker Service | net10.0 | Consumes RabbitMQ messages and routes them to the WebSocket hub |
| `Backoffice.Config.Service` | Worker Service | net10.0 | Background config processing (RabbitMQ consumer + Backoffice DB) |
| `Backoffice.Utility` | Worker Service | net10.0 | General utility jobs (RabbitMQ consumer, Azure Storage, Backoffice DB) |
| `Backoffice.Shared` | Class library | net10.0 | Shared entities, EF Core contexts, message contracts, utilities |
| `Backoffice.API.Tests` | xUnit tests | net10.0 | Tests for Backoffice.API (xUnit, Moq, EF Core InMemory) |
| `Backoffice.Web.Tests` | xUnit + bUnit tests | net10.0 | Tests for Backoffice.Web and Backoffice.WebSocket.Service |

## Prerequisites

- .NET 10 SDK. Since GM-743 (August 2026) every project targets `net10.0`, so a
  single .NET 10 SDK builds and runs everything. The Dockerfiles build on
  `mcr.microsoft.com/dotnet/sdk:10.0` and run on `aspnet:10.0`.
  With Nix (flakes enabled) and direnv hooked into your shell (`eval "$(direnv hook bash)"`),
  run `direnv allow` once and `.envrc` loads the shared `dotnet` shell from the
  `FlyingCockatoo/nix-flakes` repo on `cd`. Without direnv:
  `nix develop git+ssh://git@github.com/FlyingCockatoo/nix-flakes#dotnet`.
- Access to the private GitHub Packages NuGet feed. Every runnable project
  references `Infra.Extensions.Telemetry`, which is served from
  `https://nuget.pkg.github.com/FlyingCockatoo/index.json`. Configure the source
  once with a GitHub token that has package read access:

  ```powershell
  dotnet nuget add source "https://nuget.pkg.github.com/FlyingCockatoo/index.json" `
    --name github --username <github-username> --password <github-token> `
    --store-password-in-clear-text
  ```

- SQL Server - four databases: `Backoffice`, `Sportsbook`, `Trading`, `Translation`
  (used by Backoffice.API; `Backoffice` alone is used by Config.Service and Utility).
- RabbitMQ - consumed by the three worker services (`MessageBroker` config section).
- MongoDB - `Backoffice.API` reads a sportsbook Mongo database
  (`Custom:MongoDB-Sportsbook` connection string).
- Docker (optional) - each service has its own `Dockerfile` for container builds.

## Configuration

Each runnable project has its own `appsettings.json` plus an
`appsettings.Development.json` override (the `Development` environment is set by
the default launch profiles). Values can also be overridden with environment
variables using the standard double-underscore separator (e.g. `Security__ApiKey`).

Never commit real secrets. The committed `appsettings.json` files hold placeholders
(`${DB_PASSWORD}` inside connection strings, `${API_KEY}`, `${JWT_KEY}`,
`${RABBITMQ_PASSWORD}`, ...). .NET does not expand them, so set the real value as an
environment variable (e.g. `ConnectionStrings__Backoffice`, `Security__ApiKey`).
Deployed environments get these from argocd. Reference of the important keys
(names only - supply your own local values):

### Backoffice.API
- `ConnectionStrings:Backoffice|Sportsbook|Trading|Translation` - SQL Server databases
- `Custom:MongoDB-Sportsbook` - MongoDB connection string
- `Security:ApiKey` - the `x-api-key` value required by API controllers (`ApiKeyFilter`)
- `API:AuthenticationAPI` - internal Auth API (`Uri`, `Security:APIKey`, `JWT:Key`, `JWT:Issuer`); login requests are proxied here. Defaults to `http://localhost:40114` (the sibling `../Auth` repo)
- `API:PlatformAPI:{HK|SA}`, `API:TradingAPI:{HK|SA}`, `API:SportsbookAPI:{HK|SA}`, `API:RabbitMqApi:{HK|SA}` - upstream platform / odds-engine / sportsbook APIs, duplicated per region
- `API:WebProps` - Web Properties API (feature flags / maintenance)
- `IsStaging`, `Maintenance:Apps`, `Custom:DbContextTimeout`, `Telemetry:*`

### Backoffice.Web
- `API:Backoffice:Uri` + `API:Backoffice:Security:APIKey` - which Backoffice.API instance the UI calls (point at `https://localhost:5001` for a fully local stack)
- `API:HubAPI` - the WebSocket hub URL, ending in `/bohub` (point at `http://localhost:5190/bohub` locally)
- `Region` - `HK` or `SA`
- `Metabase:SiteUrl`, `Metabase:SsoSecretKey`, `Metabase:DashboardId`, `Metabase:EmailDomain` - the embedded dashboard and its JWT SSO
- `Metabase:ApiKey` + `Metabase:IdleCleanup:*` (`Enabled`, `IdleHours`, `IntervalMinutes`, `StartupDelaySeconds`, `GroupName`) - the job that deactivates idle Metabase accounts of the "Clients" group (GM-987). Off unless `Metabase:IdleCleanup:Enabled` is `true`. `Metabase:ApiKey` must be an API key in Metabase's Administrators group, and Metabase's JWT user provisioning must be on (it is by default) so the next login reactivates a deactivated user; supply the key as the environment variable `Metabase__ApiKey`, never in a committed file. Run it on one `bo-web` replica only, and note that DEV and STG `bo-web` share the STG Metabase
- `Tableau:*`, `SlackWebHook`, `Telemetry:*`

### Worker services (WebSocket.Service, Config.Service, Utility)
- `MessageBroker:*` - RabbitMQ host, credentials, virtual host, exchange, queue, routing key
- `ConnectionStrings:Backoffice` - Config.Service and Utility only
- `Custom:WebSocketAPI` (WebSocket.Service) - URL of Backoffice.WebSocket.API
- `Custom:AzureStorage`, `Custom:Hub` (Utility)
- `API:PlatformAPI`, `API:PlatformSportsbook`, `API:BackofficeAPI` (WebSocket.Service)

Safety notes (from `CLAUDE.md`, non-negotiable):

- Verify every connection string and `MessageBroker:Host` targets a
  non-production environment before running anything. Messages published to the
  odds-engine exchange are indistinguishable from real production events.
- `.env.*.local` files are git-ignored. `.env.bo-api.local` (see
  `docs/16_BoApiAccess.md`) holds staging credentials for ad-hoc calls against
  the deployed BO API and must never be committed.

## Running locally

Start infrastructure first (SQL Server, RabbitMQ, MongoDB), then the services.
A typical order for a full local stack:

```powershell
# 1. REST gateway - https://localhost:5001 and http://localhost:5000, Swagger UI at /swagger
dotnet run --project Backoffice.API

# 2. SignalR hub - http://localhost:5190, hub endpoint at /bohub
dotnet run --project Backoffice.WebSocket.API

# 3. Background workers (run the ones your scenario needs)
dotnet run --project Backoffice.WebSocket.Service
dotnet run --project Backoffice.Config.Service
dotnet run --project Backoffice.Utility

# 4. Web UI - https://localhost:7131 and http://localhost:5131
dotnet run --project Backoffice.Web
```

Ports come from each project's `Properties/launchSettings.json`. The workers have
no HTTP endpoint. `Backoffice.API` and `Backoffice.Web` expose a `/healthz`
health-check endpoint.

For the UI to work against your local services, set `API:Backoffice:Uri` and
`API:HubAPI` in `Backoffice.Web/appsettings.json` (or a Development override) to
your local API and hub URLs - the checked-in defaults point at deployed
staging/dev environments.

Logging in requires a reachable Auth API (`API:AuthenticationAPI:Uri`): the Web
UI posts credentials to Backoffice.API `POST /api/authentication/login`, which
proxies to the Auth API and returns a JWT. Most other API endpoints require both
an `x-api-key` header and an `Authorization: Bearer <JWT>` header - see
`docs/16_BoApiAccess.md` for the full auth model and a curl recipe.

### Docker

Each service builds standalone (build context = repo root, since
`Backoffice.Shared` is copied in). The private NuGet feed token is passed as a
build argument:

```powershell
docker build -f Backoffice.API/Dockerfile --build-arg NUGET_PASSWORD=<github-token> -t backoffice-bo-api .
```

Note: the publish step deletes `appsettings.*.json` overrides from the image, so
containers must be configured via environment variables.

## Quick start (concrete example)

A copy-paste path from a clean checkout to something visible in the browser. Two
targets are worth knowing about:

- Backoffice.API Swagger UI at http://localhost:5000/swagger - works with **no
  infrastructure at all** (no SQL, no Mongo, no RabbitMQ, no Auth API).
- Backoffice.Web login page at http://localhost:5131 - renders with the API down;
  actually logging in additionally needs Backoffice.API plus a reachable Auth API.

### Needed to start vs needed at first request

Traced against `Backoffice.API/Startup.cs` and `Backoffice.Web/Program.cs`:

| | Needed to start the process | Needed only at first request |
|---|---|---|
| Backoffice.API | .NET 10 runtime, restored NuGet packages, parseable `appsettings.json` | Everything external. The four SQL databases (`Backoffice`, `Sportsbook`, `Trading`, `Translation`) are registered as DbContexts in `Startup.cs` (lines 92-102) but no connection is opened until the first query. MongoDB connects lazily in the `SportsbookRepository`/`TradingRepository` constructors on the first request that resolves them. The Auth API and WebProps HttpClients are configured lazily on first resolution. `AddHealthChecks()` registers no database checks, so `/healthz` is green regardless. |
| Backoffice.Web | .NET 10 SDK, restored NuGet packages | Backoffice.API (`API:Backoffice:Uri`) for translations, login and all data; the SignalR hub (`API:HubAPI`) for live updates. The login page GET fetches UI translations from the API but swallows every failure and falls back to English labels (`Login.cshtml.cs` -> `TranslationService` -> `BackofficeService.GetTranslation`), so the page renders even with nothing else running. |

Logging in is the first thing that needs the full chain: Web -> Backoffice.API
`POST /api/authentication/login` -> Auth API (`API:AuthenticationAPI:Uri`, default
`http://localhost:40114`, the sibling `../Auth` repo) - plus a user provisioned in
the Auth API's own database.

### Step 0 - one-time NuGet source (all options)

```powershell
dotnet nuget add source "https://nuget.pkg.github.com/FlyingCockatoo/index.json" `
  --name github --username <your-github-username> --password <GITHUB_PAT> `
  --store-password-in-clear-text
```

`<GITHUB_PAT>` needs `read:packages` scope. Without this, `dotnet run` fails at
restore on `Infra.Extensions.Telemetry`.

### Option A - committed defaults (shared staging backends)

The checked-in `appsettings.json` files point SQL, Mongo and the upstream APIs at
shared staging hosts (`stg-song88-mssql-server...rds.amazonaws.com`,
`bo-api.stg.song88.com`, `platform-api.stg.song88.com`, ...). The passwords and API
keys in them are placeholders: export the real staging values as environment
variables first (they live in AWS SSM Parameter Store under `/stg/apps/...`, the
same parameters the argocd ExternalSecrets read). Two more caveats:

- Those hosts are network-restricted. From a machine without VPN / IP-allowlist
  access, requests to them time out (verified 2026-08: `curl
  https://bo-api.stg.song88.com/healthz` times out while general internet access
  works). None of that stops the local processes from starting.
- Staging is shared. Treat it as read-only browsing; do not exercise write
  endpoints against it (see `CLAUDE.md` Critical rules).

```powershell
# Terminal 1 - REST gateway (starts with zero infrastructure)
dotnet run --project Backoffice.API
# -> listening on https://localhost:5001 and http://localhost:5000

# Terminal 2 - Web UI
dotnet run --project Backoffice.Web
# -> listening on https://localhost:7131 and http://localhost:5131
```

Open http://localhost:5131. The Blazor router treats you as unauthenticated and
redirects to `/login` (`App.razor` -> `LoginRedirect`), so you get the Back Office
login form ("Welcome to / Back Office" banner, username/password, language
dropdown). With the committed defaults the Web UI calls `bo-api.stg.song88.com`;
point it at your local API instead by setting `API:Backoffice:Uri` to
`https://localhost:5001` (the committed `API:Backoffice:Security:APIKey` already
matches the local API's `Security:ApiKey`).

Whether login succeeds depends on the Auth API: the default
`API:AuthenticationAPI:Uri` is `http://localhost:40114`, so run the sibling repo
(`dotnet run --project ../Auth/Auth.Api`) and have a user in its database. This
repo's quick start cannot verify that step for you - if the Auth API is not
running, the login form shows an error after submit, but everything up to that
point (login page, Swagger, healthz) still works.

### Minimum browser-visible win (no infrastructure at all)

With only `dotnet run --project Backoffice.API` running:

```powershell
curl http://localhost:5000/healthz
# expected output (200 OK):
# Healthy

curl http://localhost:5000/swagger/v1/swagger.json
# expected: JSON starting {"openapi":"3.0.1","info":{"title":"Backoffice API","version":"v1"},...
```

Then open http://localhost:5000/swagger in a browser: the Swagger UI for
"Backoffice API v1" with every controller listed. This works with no databases
because nothing in `Startup.cs` touches SQL/Mongo/RabbitMQ at startup. Endpoints
that hit a database will of course fail until one is reachable.

### Option B - fully local infrastructure (Docker)

Local-only example password below - never use it anywhere shared.

```powershell
# SQL Server (the four databases live on one instance)
docker run -d --name bo-sql -p 1433:1433 `
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD="YourStrong@Passw0rd123" `
  mcr.microsoft.com/mssql/server:2022-latest

# Create the four (empty) databases - idempotent
docker exec bo-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "YourStrong@Passw0rd123" `
  -Q "IF DB_ID('Backoffice') IS NULL CREATE DATABASE [Backoffice]; IF DB_ID('Sportsbook') IS NULL CREATE DATABASE [Sportsbook]; IF DB_ID('Trading') IS NULL CREATE DATABASE [Trading]; IF DB_ID('Translation') IS NULL CREATE DATABASE [Translation];"

# RabbitMQ (only the three worker services consume it; management UI on :15672)
docker run -d --name bo-rabbit -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# MongoDB (read by Backoffice.API for sportsbook lookups)
docker run -d --name bo-mongo -p 27017:27017 mongo:6
```

Matching configuration - either environment variables:

```powershell
$env:ConnectionStrings__Backoffice  = "Data Source=127.0.0.1,1433;Database=Backoffice;User Id=sa;Password=YourStrong@Passw0rd123;TrustServerCertificate=True"
$env:ConnectionStrings__Sportsbook  = "Data Source=127.0.0.1,1433;Database=Sportsbook;User Id=sa;Password=YourStrong@Passw0rd123;TrustServerCertificate=True"
$env:ConnectionStrings__Trading     = "Data Source=127.0.0.1,1433;Database=Trading;User Id=sa;Password=YourStrong@Passw0rd123;TrustServerCertificate=True"
$env:ConnectionStrings__Translation = "Data Source=127.0.0.1,1433;Database=Translation;User Id=sa;Password=YourStrong@Passw0rd123;TrustServerCertificate=True"
dotnet run --project Backoffice.API
```

or the same keys in `Backoffice.API/appsettings.Development.json` (that file is
checked in - do not commit your local values). The Mongo key contains a dash, so
in PowerShell set it as `${env:Custom__MongoDB-Sportsbook} =
"mongodb://localhost:27017/sportsbook"` or put it in the Development override.
For the Web UI set `API:Backoffice:Uri` to `https://localhost:5001` and, if you
run the hub, `API:HubAPI` to `http://localhost:5190/bohub`.

**Honest gap - schema provisioning.** This repo contains no EF Core migrations,
no `EnsureCreated`/`Migrate` calls, and no schema creation scripts. The only SQL
in the repo is a handful of seed/permission scripts (`scripts/sql/`,
`docs/sql/`) that assume the schema already exists. The freshly created databases
above are empty: Backoffice.API still starts and serves `/swagger` and
`/healthz`, but any data endpoint fails at its first query until you provision
the schema from an existing environment (backup/restore or schema-compare from
staging). The same applies to Mongo (expects a populated `sportsbook` database)
and to login (the Auth API is a separate repo with its own database).

## API endpoints

### Backoffice.API (REST)

All routes below are verified against the controller sources in
`Backoffice.API/Controllers/`. Swagger UI (`/swagger` on a running instance) has the
full request/response schemas; the tables list route, method and a one-line purpose.

Authentication model (see `Backoffice.API/ActionFilter/`):

- **x-api-key** - every controller except `AuthenticationController` carries a
  class-level `ApiKeyFilter`; requests must send an `x-api-key` header matching
  `Security:ApiKey` or they get 401.
- **Bearer JWT** - the custom `[Authorize]` filter (not ASP.NET Core's) requires an
  `Authorization: Bearer <JWT>` header and re-validates the token against the Auth
  API on every request. Where a claim is listed (e.g. `SBC.OE.Config` Edit), the JWT
  must also carry that permission claim at the given access level.
- The custom method-level `[AllowAnonymous]` bypasses only the JWT check; the
  x-api-key filter still applies.
- `AuthenticationController` has neither filter: `login` is fully anonymous; its
  other actions return 400 unless an `Authorization` header is present (they proxy
  the token to the Auth API).
- Most Trading / Bet / Client endpoints also read an optional `x-region` header
  (`HK`/`SA`) to select the upstream region, and `x-language-code` for localised names.

`GET /healthz` - health check, anonymous.

#### AuthenticationController - `api/authentication` (no x-api-key, no [Authorize] filter)

| Method | Route | Purpose |
|---|---|---|
| POST | `api/authentication/login` | Log in (proxied to Auth API), returns JWT; anonymous |
| GET | `api/authentication/authenticate` | Validate the supplied Bearer token |
| GET | `api/authentication/logout` | Log out (invalidate token) |
| POST | `api/authentication/register` | Create a user |
| POST | `api/authentication/user/list` | List users (`?isShowSuperAdmin=`) |
| POST | `api/authentication/user/modify` | Update a user |
| POST | `api/authentication/user/modify/{userId}` | Update a user's password |
| GET | `api/authentication/user/{userId}` | Get a user |
| POST | `api/authentication/subaccount/list` | List sub-accounts |
| GET | `api/authentication/permission/list` | List permissions (`?isSubUser=`) |
| POST | `api/authentication/usergroup/list` | List user groups |
| POST | `api/authentication/usergroup/user/list` | List users in a group |
| POST | `api/authentication/usergroup/post` | Add or update a user group |
| POST | `api/authentication/usergroup/delete` | Delete a user group |
| POST | `api/authentication/activitylog/list/{userId}` | Login activity log (`?search=`) |
| POST | `api/authentication/report/roles/url` | Get report roles URL (see controller) |

#### BetController - `api/bet` (x-api-key + Bearer JWT)

| Method | Route | Purpose |
|---|---|---|
| POST | `api/bet/details` | Manual bet details |
| POST | `api/bet/manualsettlement` | Manually settle a bet |
| POST | `api/bet/resendsettlement/details` | Bet details for resend-settlement |
| POST | `api/bet/resendsettlement` | Resend a bet settlement |
| POST | `api/bet/resendprovidercancellation` | Send the Cancelled status of cancelled bets to the provider (Bet Genius), BO-125 |
| POST | `api/bet/cancel` | Cancel a bet (no `x-region`: runs HK, plus SA when not staging) |

#### AdminConfigController - `api/config` (x-api-key + JWT claim `Admin.Maintenance`)

| Method | Route | Purpose |
|---|---|---|
| GET | `api/config/maintenance` | Get maintenance state (View) |
| POST | `api/config/maintenance` | Set maintenance state (Edit) |

#### ConfigController - `api/config` (x-api-key + JWT claim `SBC.OE.Config` View; Edit where noted)

| Method | Route | Purpose |
|---|---|---|
| GET | `api/config/template/{id}` | Get config template by id |
| GET | `api/config/templates` | Search config templates (`?searchText=&sportId=&scopeId=`) |
| GET | `api/config/market-groups` | Market groups of every sport, Live and Prematch lists, from the Sportsbook table `MarketTemplateGroup2`; a sport with no rows has no entry. For the template editor (BO-142); the Config View Logs filter that also used it was removed in BO-155 |
| GET | `api/config/markets/{fixtureTypeId}/{sportId}` | Market list of a sport from the sportsbook Mongo `MarketLookup`. For the template editor (BO-142); the Config View Logs filter that also used it was removed in BO-155 |
| POST | `api/config/template` | Add or update a config template (Edit) |
| POST | `api/config/template/set/{state}` | Assign leagues to their clients (`Assigned`, with `Restricted` false = Active, true = Restricted) or unassign them (`UnAssigned`) (Edit). One row per client and league in `Trading.dbo.ClientLeagueConfig` (`IsActive = 0` is Restricted); every change goes to the activity log, always on the page `SBC.OE.Config.Leagues` whatever `x-page` the caller sent, and the Logs button of Config > Assigned / Unassigned reads that page (BO-155); HTTP 400 and nothing saved if a league belongs to a client outside the caller's client codes (BO-151) |
| POST | `api/config/template/delete/{id}` | Delete a config template (Edit) |
| POST | `api/config/template/{state}` | Get config clients/leagues by state (`?templateId=`). The Unassigned list is the sportsbook Mongo league lookup minus the leagues the client has a saved setting for in `Trading.dbo.ClientLeagueConfig`, restricted or allowed (BO-144); HTTP 400 when either cannot be read |
| GET | `api/config/template/count` | Template config counts (`?state=`); HTTP 400 when the league lookup, the client list or the saved settings behind a count fails (BO-143, BO-144), never a wrong number. The Unassigned count follows the same rule as the list and counts only the caller's clients |
| GET | `api/config/template/margin/{id}` | Get config template margin |
| POST | `api/config/template/margin` | Add or update template margin (Edit). The template editor's key and time nodes are view-only until margin per client is enabled (the odds come from the provider by default), so a save sends only the default margins and the market Active/Inactive state (BO-142) |
| POST | `api/config/template/copy/client` | Copy a client's config |
| POST | `api/config/activitylog` | Add an activity log entry |
| POST | `api/config/set/activitylog/list` | Add a batch of activity log entries |
| POST | `api/config/activitylog/list` | Query activity logs |
| POST | `api/config/activitylog/priceboostlist` | Price boost activity logs |
| POST | `api/config/activitylog/manualbetsettlementlist` | Manual bet settlement history logs |
| POST | `api/config/cachepreload` | Publish cache invalidation request (AllowAnonymous - x-api-key only) |
| GET | `api/config/translations` | BO UI translations by `x-language-code` (AllowAnonymous - x-api-key only) |

#### TranslationController - `api/translation` (x-api-key; per-action `Translations.Static` / `Translations.Dynamic` claims)

| Method | Route | Purpose |
|---|---|---|
| GET | `api/translation/sportsbook/translations` | Static translations (`?languageCode=`; Static View) |
| GET | `api/translation/sportsbook/translations/stats` | Static translation stats (Static View) |
| POST | `api/translation/sportsbook/translations` | Add or update a static translation (Static Edit) |
| POST | `api/translation/sportsbook/translations/delete` | Delete a static translation (Static Edit) |
| POST | `api/translation/sportsbook/translations/delete-key` | Delete a translation key (Static Edit) |
| POST | `api/translation/sportsbook/glossary/list` | Query dynamic glossary (Dynamic View) |
| GET | `api/translation/sportsbook/glossary/stats` | Glossary stats (Dynamic View) |
| POST | `api/translation/sportsbook/glossary` | Add or update a glossary entry (Dynamic Edit) |
| POST | `api/translation/sportsbook/glossary/processed` | Set glossary PROCESSED flag (Dynamic Edit) |
| POST | `api/translation/sportsbook/glossary/delete` | Delete a glossary entry (Dynamic Edit) |
| GET | `api/translation/sportsbook/glossary/export` | Export all glossary rows as JSON (Dynamic View) |
| POST | `api/translation/sportsbook/glossary/import` | Import parsed glossary rows (Dynamic Edit) |

#### WebPropsController - `api/config/feature-flags` (x-api-key + JWT claim `Admin.ClientSiteConfig`)

| Method | Route | Purpose |
|---|---|---|
| GET | `api/config/feature-flags/{clientCode}` | Get a client's feature flags (View) |
| POST | `api/config/feature-flags/{clientCode}` | Update a client's feature flags (Edit) |

#### TradingController - `api/trading` (x-api-key + Bearer JWT; most read `x-region`)

Most of these routes forward to the OddsEngine Trading API (`API:TradingAPI:{HK|SA}`). Since BO-42 the price boost routes (`priceboost`, `priceboost/list`, `priceboost/settings`, `priceboost/settings/list`) and the fixture lists (`sportsbook/fixtures`, `sportsbook/outrightfixtures`) are served by `TradingRepository` itself from the Trading DB (`ConnectionStrings:Trading`) and the sportsbook Mongo, and `publish` goes through the broker API of this service (`API:RabbitMqApi`). The clients of the price boost list and of Boosted Settings are the ones the Platform API lists (`IPlatformRepository.GetClients`, the list the Web builds its client dropdowns from), not a copy in the Trading database. Since BO-141 the client list (`client`) is answered the same way, from the Platform client list filtered to the caller's client codes.

| Method | Route | Purpose |
|---|---|---|
| GET | `api/trading/sports/{fixtureTypeId}` | Sports for a fixture type |
| GET | `api/trading/sports/topmarkets/{sportId}` | Top markets for a sport |
| GET | `api/trading/sports/lookup` | Sport lookup |
| GET | `api/trading/sports/selectormarkettemplates` | Selector market templates |
| GET | `api/trading/segment/lookup/{sportId}` | Segment lookup |
| GET | `api/trading/league/lookup/{sportId}/{segmentId}` | League lookup |
| POST | `api/trading/tier/leagueorder/list` | Tier league order list |
| GET | `api/trading/fixtures/{fixtureTypeId}/sports/{sportId}/segments/{segmentId}/leagues/{leagueId}/markets/{marketTemplateId}/most-even-line` | Fixtures with most-even line for a market |
| GET | `api/trading/fixtures/market/{fixtureTypeId}/{sportId}/{segmentId}/{leagueId}` | Fixtures by market |
| GET | `api/trading/fixtures/market/{fixtureTypeId}/{sportId}/{segmentId}/{leagueId}/{marketTemplateId}` | Fixtures for one market template |
| GET | `api/trading/fixtures/market/selections/{fixtureTypeId}/{providerFixtureId}/{providerMarketId}/{providerSelectionId}` | Fixture market selections |
| GET | `api/trading/fixtures2/market/details/{providerFixtureId}` | Fixture market details |
| POST | `api/trading/sportsbook/fixtures` | Match fixtures with their markets and selections, English names, earliest first (read from the sportsbook Mongo; `AllFixtureStatus` includes started and closed fixtures; a fixture is listed whether or not it carries a client code, `ClCo`) |
| POST | `api/trading/sportsbook/outrightfixtures` | Prematch outright fixtures with their markets and selections, in league tier order (read from the sportsbook Mongo) |
| POST | `api/trading/priceboost` | Add or update price boost configs (a different selection of the same market replaces the saved one; zero or empty odds delete it) |
| POST | `api/trading/priceboost/list` | Price boost config list with the live fixture, market and odds of each boost; a market published without client margins (`ClCoMa`) gives every client the provider odds (also deletes the boosts of fixtures that ended) |
| POST | `api/trading/priceboost/settings/list` | Boosted settings list |
| POST | `api/trading/priceboost/settings` | Add or update the % cap of a client (`?userId=`), then notifies that user with a toast |
| GET | `api/trading/client` | Client list from the Platform client list, limited to the caller's client codes (BO-141) |
| POST | `api/trading/client/config/all` | Get client config (all) |
| POST | `api/trading/v2/client/config` | Get client config (v2) |
| POST | `api/trading/client/config` | Add or update client config |
| POST | `api/trading/client/config/audit` | Client config audit logs |
| GET | `api/trading/client/leagueconfig` | Client league config (`?clientCode=`) |
| GET | `api/trading/client/marketconfig` | Client market config (`?clientCode=`) |
| POST | `api/trading/client/marketconfig/{pageSize}/{page}` | Client market config, paged (`?clientCode=`) |
| POST | `api/trading/bet/selections` | Bet selection details |
| POST | `api/trading/bet/details` | Manual bet details |
| GET | `api/trading/fastodds/sports/{fixtureTypeId}` | Fast odds sports |
| POST | `api/trading/fastodds/fixtures` | Fast odds fixtures |
| POST | `api/trading/manual/marketsettlement` | Add or update manual market settlements (`?userId=`) |
| POST | `api/trading/manual/marketsettlement/history` | Market settlement history (claim `Admin.Settlements.MarketSettlement` View) |
| POST | `api/trading/manual/marketsettlement/details` | Manual market settlement details |
| POST | `api/trading/manual/messagesettlement` | Add manual message settlements |
| POST | `api/trading/manual/messagesettlement/list` | Message settlement audit |
| GET | `api/trading/marketlookup/{fixtureTypeId}/{sportId}` | Market lookup |
| POST | `api/trading/publish` | Publish a message through the broker API (`API:RabbitMqApi`, see controller) |
| GET | `api/trading/bookmakers` | Bookmaker list |
| POST | `api/trading/notifications` | Query notifications |
| POST | `api/trading/markettemplateplayerprops` | Market template player props |
| GET | `api/trading/markettemplategroups/{fixtureTypeId}` | Market template groups |
| POST | `api/trading/fixture/market/deactivate` | Deactivate fixture markets |
| GET | `api/trading/mapping/sports/{sportId}`, `.../segments/{segmentId}`, `.../leagues/{leagueId}`, `.../fixtures/{fixtureId}`, `.../markets/{marketId}`, `.../selections/{selectionId}` | Provider mapping by id (6 routes) |
| DELETE | `api/trading/mapping/sports` | Delete sport mapping (`?sportId=&userId=`) |
| POST | `api/trading/mapping/sports`, `.../segments`, `.../leagues`, `.../fixtures`, `.../markets`, `.../selections` | Search provider mappings (6 routes) |
| GET | `api/trading/unmapped/sports`, `.../segments`, `.../leagues`, `.../fixtures`, `.../markets`, `.../selections` | Unmapped entities (6 routes) |
| POST | `api/trading/mapping/{category}` | Add or update a provider mapping (the 6 literal POST routes above take precedence over this parameter route) |
| POST | `api/trading/mapping/{category}/bulk` | Bulk add or update provider mappings |

#### SettingsController - `sportsbook/settings` (x-api-key + Bearer JWT)

| Method | Route | Purpose |
|---|---|---|
| POST | `sportsbook/settings/topfixtures` | Get client top fixtures |
| GET | `sportsbook/settings/topfixtures/sportstree` | Sports tree for a client (`?clientId=`) |
| POST | `sportsbook/settings/topfixtures/post` | Save client top fixtures |
| POST | `sportsbook/settings/topfixtures/copy` | Copy top fixtures between clients |
| POST | `sportsbook/settings/allleagues` | All leagues for a client |
| POST | `sportsbook/settings/topleagues` | Get top leagues |
| POST | `sportsbook/settings/topleagues/post` | Save top leagues |
| POST | `sportsbook/settings/topleagues/copy` | Copy top leagues between clients |
| GET | `sportsbook/settings/sports` | Client sports (`?LanguageCode=`) |
| GET | `sportsbook/settings/parlaybonus/list` | Get client parlay bonus |
| POST | `sportsbook/settings/parlaybonus` | Add or update parlay bonus |
| POST | `sportsbook/settings/announcements` | Get announcements |
| POST | `sportsbook/settings/announcements/post` | Add or update announcements |
| POST | `sportsbook/settings/announcements/delete` | Delete announcements |

#### ClientController - `sportsbook/client` (x-api-key + Bearer JWT)

| Method | Route | Purpose |
|---|---|---|
| GET | `sportsbook/client/list` | List clients |
| POST | `sportsbook/client/information/list` | Client information list, filtered by the JWT's ClientCodes claim |
| POST | `sportsbook/client/post` | Add or update a client |
| POST | `sportsbook/client/bet/setting` | Get bet settings |
| GET | `sportsbook/client/exchange/rates` | Exchange rates |
| GET | `sportsbook/client/platform/list` | Platform client list |
| GET | `sportsbook/client/parentmap` | Client parent map (AllowAnonymous - x-api-key only; used service-to-service by WebSocket.Service) |
| POST | `sportsbook/client/markettemplategroups` | Get client market template groups |
| POST | `sportsbook/client/markettemplategroup/post` | Add or update a market template group |
| POST | `sportsbook/client/markettemplategroup/delete` | Delete a market template group |
| POST | `sportsbook/client/markettemplategroup/copy` | Copy market template groups |
| POST | `sportsbook/client/sportsettings` | Add or update client sport settings |
| POST | `sportsbook/client/sportorder/list` | Client sport order list |
| POST | `sportsbook/client/sportorder/audit/list` | Sport order audit list |
| POST | `sportsbook/client/segmentorder/list` | Client segment order list |
| POST | `sportsbook/client/leagueorder/list` | Client league order list |
| POST | `sportsbook/client/oddstableleagueorder/list` | Client Oddstable league order list (BO-107), filtered by client and sport |
| POST | `sportsbook/client/oddstableleaguesettings` | Replace a client's Oddstable league order for one sport (BO-107) |
| POST | `sportsbook/client/oddstableleaguesettings/copy` | Copy one client's Oddstable league order for a sport to other clients (BO-107) |
| POST | `sportsbook/client/oddstableleaguesettings/import` | Import a League Sorting CSV for one client and sport (BO-122). `Commit = false` only validates; `Commit = true` saves, audits (`Import`) and publishes the cache-sync message. Matched leagues take the first positions in the file's `SortOrder`, the client's other leagues follow in their current order, unknown leagues are skipped and reported |
| POST | `sportsbook/client/oddstableleaguesettings/logs` | League Sorting audit entries from `ActivityLog2`, newest first, paged (BO-107) |
| POST | `sportsbook/client/tier/leagueorder/list` | Tier league order list |
| GET | `sportsbook/client/markettemplate/{clientCode}/{sportId}/{fixtureStatusId}` | Client market templates |
| POST | `sportsbook/client/markettemplate` | Add or update market template order |
| POST | `sportsbook/client/markettemplate/copy` | Copy a client's market sort order and market groups to one or more clients (BO-159) |
| GET | `sportsbook/client/selectormarkettemplate/{clientId:guid}` | Client selector market templates |
| GET | `sportsbook/client/selectormarkettemplate/lookup` | Selector market template lookup |
| POST | `sportsbook/client/selectormarkettemplate` | Add or update selector market templates |
| POST | `sportsbook/client/selectormarkettemplate/copy` | Copy selector market templates |
| GET | `sportsbook/client/sports/lookup` | Sport lookup |
| GET | `sportsbook/client/segment/lookup/{sportId}` | Segment lookup |
| GET | `sportsbook/client/league/lookup/{sportId}/{segmentId}` | League lookup |

### Backoffice.WebSocket.API (SignalR)

Hub endpoint: `/bohub` (`Backoffice.WebSocket.API/Program.cs`), MessagePack protocol,
transports WebSockets / SSE / long polling, plus `GET /healthz`. No authentication is
configured on the hub. A `?userId=` query-string parameter on connect registers the
connection for user-targeted notifications.

Client-invokable hub methods (`Hubs/BackofficeHub.cs`) - the `Publish*` methods are
called by `Backoffice.WebSocket.Service` to fan messages out to groups:

| Hub method | Purpose |
|---|---|
| `SubscribeToChannel(string)` | Join a group (lower-cased channel name) |
| `UnSubscribeToChannel(string)` | Leave a group |
| `PublishMonitoringOddsChange(payload)` | Fan out monitoring odds change to the payload's groups |
| `PublishOddsChange(payload)` | Fan out fixture-details odds change to the payload's groups |
| `PublishBetStopFixture(payload)` | Fan out bet-stop for a fixture to the payload's groups |
| `PublishBetStopGlobal(payload)` | Fan out global bet-stop to the `onbetstopglobal` group |
| `PublishNotification(payload)` | Notify one user (by userId), the payload's groups, or all clients |

Server-to-client events (`Events/IBackofficeEvent.cs`):

| Event | Payload type |
|---|---|
| `OnOddsChange` | `OddsChangeFixtureDetailsMessage` |
| `OnMonitoringOddsChange` | `OddsChangeMonitoringMessage` |
| `OnBetStopFixture` | `BetStopFixtureMessage` |
| `OnBetStopGlobal` | `BetStopGlobalMessage` |
| `OnNotification` | `NotificationsModel` |

## Running tests

Two test projects exist (xUnit; `dotnet test` discovers both from the solution):

```powershell
# Everything
dotnet test

# Individually
dotnet test Backoffice.API.Tests     # API unit tests (Moq, EF Core InMemory)
dotnet test Backoffice.Web.Tests     # Web component tests (bUnit) + WebSocket.Service tests
```

On pull requests, CI (`.github/workflows/ci.yml`) builds the solution with warnings as
errors (NuGet vulnerability warnings NU1900-NU1905 excepted), then runs the test suite and
`dotnet format --verify-no-changes`. A push to `main` runs no tests, so still run them locally
before pushing.

## Scripts

- `scripts/Test-BackofficeAPIs.ps1` - health-checks the external APIs consumed by
  the solution (Platform API, Trading API) using the configuration in
  `Backoffice.API/appsettings.json`. The API key there is a placeholder, so set
  `API__PlatformAPI__HK__APIKey` (or `__SA__`) first. Run it before debugging Web
  errors to rule out upstream failures. Supports `-Region HK|SA`.
- `scripts/New-ReleaseTag.ps1` - creates an incremental GitHub Release
  (`vMAJOR.MINOR.PATCH-stg|prd`) to trigger the CI deployment; a plain git tag
  does not deploy.

## Deployment

GitHub Actions (`.github/workflows/ci.yml`) builds a Docker image per service on
every push/PR to `main`, and on a published GitHub Release tagged `*-stg` or
`*-prd` pushes the images to the environment's AWS ECR registry. Kubernetes /
ArgoCD manifests live in the sibling `../argocd` repository.

## Static analysis

`.github/workflows/sonarqube.yml` runs a SonarQube scan (project key
`backoffice-final`, https://sonarqube.stg.song88.com, Twingate required) on every
push to `main`. Vendored third-party scripts that are copied into the repo
unminified (`wwwroot/js/jquery-ui.js`, `wwwroot/js/jquery.multisortable.js`) are
excluded via `sonar.exclusions`, since we do not modify them. Add any newly
vendored library to that list rather than fixing its findings.

## Related repositories

Sibling repos in the same parent directory (see `docs/15_ReferenceRepositories.md`):
`../Auth` (JWT issuance), `../platform-final` (Platform API and services),
`../odds-engine-2` (odds engine), `../oddsfeed` (settlement), `../platform-web`
(player-facing frontend), `../webprops-api` (feature flags / maintenance),
`../argocd` (deployment configs).

## Keeping this README up to date

This README is part of the codebase. Update it in the same PR whenever anything it documents changes - project structure (projects/services added, renamed, or removed), configuration keys, run commands, ports, endpoints, or external dependencies. An outdated run guide is worse than none.
