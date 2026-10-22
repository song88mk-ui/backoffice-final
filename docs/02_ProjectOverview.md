# Project Overview

**Document Status:** Analysis Complete (No Code Changes Made)  
**Date:** June 18, 2026

## Project Inventory

| Project | Type | Purpose | Status |
|---------|------|---------|--------|
| Backoffice.API | ASP.NET Core Web API | REST API gateway | Production |
| Backoffice.Web | ASP.NET Core Blazor Server | Web UI | Production |
| Backoffice.WebSocket.API | ASP.NET Core Web API | Real-time hub | Production |
| Backoffice.Config.Service | Worker Service | Config background processor | Production |
| Backoffice.Utility | Worker Service | General utility processor | Production |
| Backoffice.WebSocket.Service | Worker Service | Message router to clients | Production |
| Backoffice.Shared | Class Library | Shared entities, models, utilities | Production |

---

## 1. Backoffice.API

### Purpose
Primary REST API gateway for all backoffice operations. Orchestrates data access, external service communication, and message publishing.

### Framework
- ASP.NET Core (.NET 10)
- .NET 10

### Key Components

#### Controllers (7 main groups)
- **AuthenticationController** - Login, token management
- **ConfigController** - Configuration templates, settings
- **ClientController** - Client management (sportsbook)
- **SettingsController** - Client settings, market templates
- **TradingController** - Trading mappings, settlements
- Plus support for activity logging and other operations

#### Repositories (8 implementations)

1. **IBackofficeRepository / BackofficeRepository**
   - Core business logic
   - Activity logs, client info, configuration templates
   - Database transactions with TransactionScope
   - ~2000+ lines of complex business logic

2. **ITradingRepository / TradingRepository**
   - Trading-specific operations
   - Market/league/fixture/selection mappings
   - Settlement history queries
   - Provider mapping management

3. **IPlatformRepository / PlatformRepository**
   - Client and configuration delegation to Platform API
   - Top leagues/fixtures queries
   - Exchange rates retrieval
   - Multi-region support (HK, SA)

4. **ISportsbookRepository / SportsbookRepository**
   - Fixture and market data queries
   - MongoDB and SQL Server integration
   - Client market template management
   - Sport/league/segment lookups

5. **IAuthenticationRepository / AuthenticationRepository**
   - External Auth API delegation
   - User login and token refresh
   - Token validation

6. **IRabbitMqRepository / RabbitMqRepository**
   - Message publishing to RabbitMQ
   - HTTP-based RabbitMQ API communication
   - Region-aware message routing

7. **IMaintenanceRepository / MaintenanceRepository**
   - Maintenance API delegation (stub)

7a. **IWebPropsRepository / WebPropsRepository**
   - Delegates to the sibling `webprops-api` service (`../webprops-api/`) for per-client site config: feature flags, game tracker product names, per-layout themes, Euro layout
   - GET/POST `/api/v1/config/{clientId}` (full-replace POST; repository round-trips the JSON so unmodeled fields - and modeled fields the caller leaves null - are preserved)
   - Backs the `Backoffice.Web` "Client Site Config" admin page (`Pages/Administration/ClientSiteConfig.razor`; renamed from "Feature Flags" in GM-696, claim `Admin.ClientSiteConfig`; editable themes/game tracker/Euro layout added in GM-697; activity log viewer modal + compact flag grid added in GM-700 — history read via the existing `api/config/activitylog/list`, diffed client-side in `ClientSiteConfigLogsModal`)

8. **IBackofficeActivityLogRepository / BackofficeActivityLogRepository**
   - Activity logging to RabbitMQ
   - Audit trail publishing

#### Action Filters
- **ApiKeyFilter** - x-api-key header validation
- **AuthorizeAttribute** - JWT token and claim validation

#### Models
- Request/Response envelopes with BaseResponse<T>
- Domain-specific models (ClientInfo, BetSettings, ConfigTemplate)
- Outgoing message definitions

### Dependencies
- Backoffice.Shared (shared library)
- Entity Framework Core 10.0.11
- Newtonsoft.Json (legacy) + System.Text.Json
- RabbitMQ.Client 5.2.0
- Serilog 4.3.0
- OpenTelemetry 1.15.3 (via Infra.Extensions.Telemetry 0.1.1-5)

### Startup Configuration

**Program.cs Pattern:**
- Legacy: Uses Startup.cs class

**Startup.cs Configuration:**
```
1. Telemetry/Instrumentation setup
2. DbContexts with 4 separate databases
3. Repository registration (scoped/transient)
4. HttpClient configurations for external APIs
5. CORS policies (allow any origin)
6. Swagger/OpenAPI setup
7. Security: API key filter
```

### Database Access
- 4 DbContexts: Backoffice, Trading, Sportsbook, Translation
- Connection pooling with DbContextPool
- 180-second timeout for long-running queries
- No migrations visible (managed elsewhere)

### External Service Calls
- Platform API - Client and configuration management
- Sportsbook API - Fixture and market data
- Authentication API - User authentication
- Trading API - Odds and market settings
- Maintenance API - System maintenance
- webprops-api - Per-client feature flags (sibling repo, S3-backed, see `docs/15_ReferenceRepositories.md`)

### Message Publishing
- All to RabbitMQ via IRabbitMqRepository
- Fire-and-forget pattern (not awaited)
- Async publishing but result not handled

### Error Handling
- Try-catch in all repositories
- Returns BaseResponse with StatusCode enum
- Logs errors to Serilog
- No global exception middleware

### Performance Considerations
- Multiple HttpClient creations (not reused)
- No query optimization visible
- Large repository classes (single responsibility violated)
- Memory caching for expensive queries

---

## 2. Backoffice.Web

### Purpose
Server-side Blazor web application providing UI for backoffice operations. Real-time updates via SignalR.

### Framework
- ASP.NET Core (.NET 10)
- Blazor Server
- SignalR

### Key Components

#### Startup (Program.cs using Minimal APIs)
```csharp
- Serilog logging configuration
- Telemetry setup
- DbContext registration (none, API calls only)
- Custom service registration via extensions
- HTTP client configuration
- Session and local storage support
```

#### Services
- **ICommonService / CommonService** - Business logic, formatting, claim processing
- **ITradingService / TradingService** - Trading-specific operations
- **MetabaseService** - Metabase SSO integration
- **MetabaseIdleUserCleanupService** (GM-987) - `BackgroundService` that deactivates idle Metabase accounts of the "Clients" group so the Metabase bill follows concurrent use; off unless `Metabase:IdleCleanup:Enabled` is true (see `docs/04_StartupFlow.md`)
- **MetabaseAdminClient / IMetabaseAdminClient** (GM-987) - the three Metabase admin API calls the cleanup needs (find group, list active members, deactivate), authenticated with `Metabase:ApiKey`
- **LeagueSortingHelper** - Pure row building, filtering, validation and drag-reorder logic behind the League Sorting page (BO-107)
- **LeagueSortingCsvHelper** - Builds and parses the League Sorting export/import CSV (BO-122); reuses the escaping and RFC4180 parser of `GlossaryCsvHelper`

#### Features
- **League Sorting** (BO-107) - `Pages/OddsEngine/LeagueSorting.razor`, route `/hk/odds-engine/league-sorting`, claim `SBC.OE.LeagueSorting`. Per-client, per-sport Oddstable league order: client list with a pill preview, an edit dialog (drag to reorder, searchable league picker, add/remove rows) and a multi-client copy with a confirmation step. Reached from the Sports Settings dropdown in the Odds Engine header. Data goes through `sportsbook/client/oddstableleague*` into `ClientOddstableLeagueOrder`, kept separate from the Sport Tree order. Every save and copy is audited by `SportsbookRepository` into `ActivityLog2` (`Page = SBC.OE.LeagueSorting`, `Payload` = client code, `Activity` = Add / Edit / Reorder / Copy, `From` / `To` = `OddstableLeagueSortingAudit` JSON with the previous and new order and, for a copy, the source client); the Logs dialog reads them back through `IBackofficeActivityLogRepository.GetPagedActivityLogs`. Import/Export (BO-122): each client row has an Export icon (View and Edit access) that downloads the client's leagues for the selected sport as `LeagueSorting-{client}-{sport}-{time}.csv` (header `ClientCode,SportId,SegmentId,SegmentName,LeagueId,LeagueName,SortOrder`; a client without leagues gets a header-only template) and an Import icon (Edit access) that opens `Components/OddsEngine/LeagueSorting/ImportModal.razor`: choose the CSV, the server validates it (`POST .../oddstableleaguesettings/import` with `Commit = false`, nothing saved), the dialog lists the leagues in the file, the skipped ones with a reason and the counts, then Import commits the same rows and shows the result. A file made for another client or sport, or with a bad header or number, is refused before anything is sent. Matching is by `SegmentId` + `LeagueId` against the sport's league lookup (names in the file are ignored); the merge rule lives in `Backoffice.API/Repositories/OddstableLeagueImportPlanner.cs` (matched leagues take the first positions in the file's `SortOrder`, the client's other leagues follow in their current order, positions renumbered from 1, at most 50 leagues, duplicate league or `SortOrder` below 1 rejects the whole file). An import that leaves the order unchanged saves, audits and publishes nothing. The import audit entry uses `Activity = Import`; Export is a read of what is already on the page and is not audited
- ServerSideBlazor with DetailedErrors enabled
- HTTP client factories for API calls
- Local storage via Blazored.LocalStorage
- Session management
- Authorization extensions
- State management

#### Middleware Pipeline
```
1. Response compression
2. Developer exception page (always on)
3. Session middleware
4. Static files
5. Routing
6. Blazor Hub mapping
7. Fallback to _Host page
```

#### Configuration
- HTTP clients for Backoffice.API calls
- HTTP clients for external service calls
- Authorization policies
- State providers

### Performance Characteristics
- Server-side rendering (CPU intensive)
- All clients share server memory
- Circuit options: DetailedErrors=true, 3-minute retention
- Response compression enabled

### Issues
- `UseDeveloperExceptionPage()` hardcoded (never disabled)
- Exception handler commented out
- HSTS disabled
- `// DPK TODO: configure dpk` comment left in code

---

## 3. Backoffice.WebSocket.API

### Purpose
Real-time bidirectional communication hub using SignalR with WebSocket protocol and fallback transports.

### Framework
- ASP.NET Core (.NET 10)
- SignalR

### Key Components

#### BackofficeHub
- User connection tracking via dictionary with locks
- Group-based subscriptions
- Method broadcasting to groups/users
- ConnectionId tracking

#### Message Types Published
- MonitoringOddsChange
- OddsChange
- BetStopFixture
- BetStopGlobal
- Notification

#### Transports Supported
- WebSocket (primary)
- Server-Sent Events (fallback)
- Long Polling (last resort)

#### Serialization
- MessagePack (binary, compressed)
- ContractlessStandardResolver
- 5MB message buffer limit

#### Health Checks
- `/healthz` endpoint mapped

### Configuration
```
SignalR with MessagePack protocol
Circuit retention: 3 minutes for disconnected clients
Max message size: 5MB
```

### Thread Safety Concerns
- Static `_users` dictionary with lock() protection
- Lock is on object, not optimized
- Potential for lock contention under high load

---

## 4. Backoffice.Config.Service

### Purpose
Background worker service consuming RabbitMQ messages for configuration updates.

### Framework
- ASP.NET Core Worker Service
- .NET 10

### Key Components

#### Worker Pattern
- Inherits BackgroundService
- Implements ExecuteAsync
- Message pump from RabbitMQ

#### Message Processing
```
Message Types:
- UpdateClientMargin: Updates configuration margins
- DeleteConfigTemplate: Removes configuration templates
```

#### Database Access
- IBackofficeRepository for database operations
- Scoped lifetime
- No DbContext pooling

#### Message Broker
- IMessageBroker for consuming
- Ack/Nack pattern for message acknowledgment
- Error logging

### Startup Configuration
```
1. Serilog setup
2. Telemetry/Instrumentation
3. IMessageBroker singleton
4. IBackofficeRepository singleton (potential issue)
5. Worker as hosted service
```

### Issues
- Repository instantiated with direct DbContext creation
- No async/await for database operations
- Singleton repository (not scoped)

---

## 5. Backoffice.Utility

### Purpose
General-purpose background worker for settlement history exports, configuration bulk operations, and report generation.

### Framework
- ASP.NET Core Worker Service
- .NET 10

### Key Components

#### Worker Pattern
- BackgroundService implementation
- Message pump from RabbitMQ
- Correlation ID tracking

#### Message Processing (10+ types)
- LogActivity / LogActivityList
- ExportMessageSettlementHistory
- ExportManualBetSettlementHistory
- ExportMarketSettlementHistory
- ExportOeConfigTemplate
- BulkUpdateConfig
- CopyClientSettings
- ExportDeletedTemplate

#### Distributed Tracing
- Baggage and activity context propagation
- Activity recording with tags:
  - messaging.system
  - messaging.destination_kind
  - messaging.rabbitmq.routing_key
  - messaging.rabbitmq.message_type

#### Repository
- IBackofficeRepository for data access

### Startup Configuration
```
1. Serilog with structured logging
2. Telemetry with full OpenTelemetry setup
3. IMessageBroker singleton
4. IBackofficeRepository singleton
5. Worker as hosted service
```

### Features
- Full activity tracking and tracing
- Correlation ID propagation
- Error handling with retry logic
- Structured logging for audit trail

---

## 6. Backoffice.WebSocket.Service

### Purpose
Routes messages from RabbitMQ to connected WebSocket clients via SignalR hub.

### Framework
- ASP.NET Core (likely Web API or Worker)

### Key Components

#### Message Consumer
- RabbitMQ consumer pattern
- Message type routing
- WebSocket hub communication

#### Broadcasting
- Sends to BackofficeHub
- Group-based or broadcast delivery
- MessagePack serialization

### Startup
- Message broker connection
- Hub client proxy setup
- RabbitMQ consumer configuration

---

## 7. Backoffice.Shared

### Purpose
Shared library containing all domain models, database contexts, utilities, and message definitions.

### Framework
- .NET Standard Class Library
- .NET 10 compatible

### Key Subdirectories

#### Data/
- **BackofficeContext** - ActivityLog, ClientInformation, BetSettings, ConfigTemplates
- **TradingContext** - Sport/League/Market/Selection mappings
- **SportsbookContext** - Fixtures, Markets, Selections, Lookups
- **TranslationContext** - Multi-language content

#### Models/
- **Request Models** - Input contracts
- **Response Models** - Output contracts
- **Domain Models** - Business entities
- **BaseResponse<T>** - Universal response wrapper

#### Enums/
- StatusCode (Success, Error, Validation_Error, Not_Found)
- Activity (audit types)
- BetStatus (Pending, Settled, Cancelled)
- Currency (BTC, ETH, USD, EUR, etc.)
- Scope (Prematch, Live)

#### Utilities/
- **MemCache** - In-process memory caching
- **MessageBroker** - RabbitMQ wrapper
- **JwtParser** - Token parsing
- **SportHelper** - Domain helpers

#### Outgoing/Incoming Messages/
- Engine messages for odds updates
- Settlement notifications
- Configuration changes
- Cache invalidation commands

#### Extensions/
- DictionaryExtensions
- DateTimeExtension
- Custom utilities

### Dependencies
- MongoDB.Driver 3.12.0
- RabbitMQ.Client 5.2.0
- Entity Framework Core 10.0.11
- OpenTelemetry.Api 1.15.3
- System.Runtime.Caching 7.0.0
- Newtonsoft.Json 13.0.1

### Database Contexts

All use SQL Server with Entity Framework Core:

| Context | Tables | Purpose |
|---------|--------|---------|
| BackofficeContext | 7 | Operational data |
| TradingContext | N/A | Trading data |
| SportsbookContext | Collections | Fixtures, markets |
| TranslationContext | N/A | Multi-language |

---

## Project Dependencies Summary

```
Backoffice.Web
    ├─→ Backoffice.Shared
    └─→ (HTTP calls to Backoffice.API)

Backoffice.API
    ├─→ Backoffice.Shared
    ├─→ (External APIs via HttpClient)
    ├─→ RabbitMQ
    └─→ SQL Server + MongoDB

Backoffice.WebSocket.API
    ├─→ Backoffice.Shared (for messages)
    └─→ (HTTP calls from Web)

Backoffice.Config.Service
    ├─→ Backoffice.Shared
    ├─→ RabbitMQ (consumer)
    └─→ SQL Server

Backoffice.Utility
    ├─→ Backoffice.Shared
    ├─→ RabbitMQ (consumer)
    └─→ SQL Server

Backoffice.WebSocket.Service
    ├─→ Backoffice.Shared
    ├─→ RabbitMQ (consumer)
    └─→ SignalR Hub

Backoffice.Shared
    └─→ (No internal dependencies)
```

---

## Build & Runtime Characteristics

### Build
- All projects compile to .NET 10 binaries
- Single solution file: backoffice-final.sln
- Embedded resource files, migrations
- NuGet dependencies resolved at build time

### Runtime
- Multi-process deployment model
- Each service independently runnable
- Shared configuration via appsettings.json files
- Environment-specific overrides supported

### Container Deployment
- Each project has Dockerfile
- Stateless design (except SignalR connections)
- Environment variables for secrets
- Health check endpoints

---

## Maturity Assessment

### Stable Components
- Core API architecture
- Database access patterns
- Authentication/Authorization
- Logging infrastructure

### Areas Under Development
- Activity logging (marked for removal)
- Error handling (inconsistent)
- HTTP client handling (patterns evolving)
- Configuration validation (TODO comments present)

### Experimental/Test Code
- "Note: Will Remove" comments
- TODO items left in code
- Hardcoded values in some places
- Legacy serialization libraries

---

## Technology Debt

### High Priority
1. ~~.NET Core 3.1 end-of-life~~ — resolved August 2026 (GM-743): all projects upgraded to .NET 10
2. Unused/deprecated code marked for removal
3. Mixed JSON serialization libraries
4. Developer exception page always enabled

### Medium Priority
1. HttpClient handling (creating new instances)
2. Error handling consistency
3. Configuration validation at startup
4. Repository class sizes (single responsibility)

### Low Priority
1. Thread safety improvements
2. Performance optimizations
3. Code documentation
4. Test coverage

