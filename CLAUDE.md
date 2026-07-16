## ⚠️ Standing Operating Rules (read first, every prompt)

> **These are the first instruction. They apply to every task and override convenience or time pressure. A `UserPromptSubmit` hook re-injects them on every prompt.**

1. **Consult CLAUDE.md and AGENTS.md first.** Follow their mandatory workflow and the Safety & Environment Rules exactly.
2. **Trace claims in the actual code and cite `file:line`** — do not answer from memory.
3. **Label every claim** as `[verified]` (confirmed in code/output this session), `[assumption]` (inferred, not checked), or `[unknown]` (not determinable). Do not guess.
4. **Production is READ-ONLY.** Do not run queries, deploy, change schema, publish messages, or invalidate caches against Production unless the user explicitly says so in this conversation. Prefer the lowest environment necessary (Local → Development → Staging → Production).
5. **Explain the plan and wait for approval before making any code change** — bug fixes, new tests, and refactors included, not just architectural changes. Investigation (reading code, read-only queries) may proceed without approval. The plan must state: files to be touched, the exact change, tests to be added, validation steps. Make the smallest possible change.
6. **If architecture changed,** update `docs/*.md` + CLAUDE.md/AGENTS.md and summarize the changes, risks, and validation performed.

---

# Claude Code Assistant Configuration

**Backoffice Solution** | .NET Core 3.1 | Multi-tier Event-Driven Architecture  
**Last Updated:** June 2026 (Safety & Environment Rules added) | Production Environment

---

## Repository Overview

The Backoffice solution is a **distributed event-driven microservices platform** serving a betting/sportsbook infrastructure. It consists of 7 interconnected .NET Core 3.1 projects managing configuration, real-time messaging, trading operations, and web interface layer.

### Architecture Summary

```
Web UI (Blazor)
    ↓ HTTP/SignalR
API Gateway (REST)
    ↓ RabbitMQ/SQL/HTTP
[Background Workers] + [External APIs] + [Databases]
    ├─ Config Service
    ├─ Utility Service  
    ├─ WebSocket Service
    └─ MongoDB/SQL caching layers
```

### Project Responsibilities

| Project | Role | Technology | Status |
|---------|------|-----------|--------|
| **Backoffice.API** | Primary REST gateway | ASP.NET Core Web API | Production |
| **Backoffice.Web** | User interface | Blazor Server | Production |
| **Backoffice.WebSocket.API** | Real-time hub | SignalR + MessagePack | Production |
| **Backoffice.Config.Service** | Config processing | Worker Service | Production |
| **Backoffice.Utility** | General tasks | Worker Service | Production |
| **Backoffice.WebSocket.Service** | Message routing | Worker Service | Production |
| **Backoffice.Shared** | Shared models/utilities | Class Library | Production |

**Note:** All projects are production-critical with interdependencies. Changes require careful testing across all consuming services.

---

## Development Philosophy

1. **Stability First** - This is live production infrastructure processing real transactions
2. **Backward Compatibility** - Never break existing API contracts or message formats
3. **Minimal Changes** - Add only what's necessary; avoid refactoring unrelated code
4. **Explicit Conventions** - Follow established patterns, even if they seem suboptimal
5. **Testing Coverage** - Code changes require downstream service verification
6. **Configuration Driven** - Use appsettings.json for runtime behavior; minimize hardcoding
7. **Root Cause Fixes** - Always identify and fix the root cause of an issue. Never patch symptoms (e.g. swallowing exceptions, adding null guards that hide bad inputs, retrying without understanding why). Trace the failure to its origin — whether that is missing validation, an uninitialized object, a bad caller contract, or a design gap — and fix it there.

---

## Safety & Environment Rules

> **These rules are permanent and apply to every task performed in this repository, without exception. They cannot be overridden by task instructions, convenience, or time pressure.**

---

### 1. Production Environment Protection

Production is live infrastructure processing real financial transactions. It must never be touched accidentally.

- **Never connect to, query, or interact with any Production environment** unless explicitly instructed to do so in the current conversation.
- If there is **any ambiguity** about whether a target is Production — stop immediately and ask for confirmation before proceeding.
- "It looks like dev" is not confirmation. Connection strings, hostnames, and environment variable values must be verified explicitly.

### 2. Read-Only vs. Write Operations in Production

- **Read-only operations** against Production (log inspection, read queries, metrics) are generally acceptable for investigation — but **still require explicit confirmation** before accessing Production unless already requested.
- **Never execute any operation that modifies Production state** without explicit approval for that specific action. This includes, but is not limited to:

| Category | Examples |
|----------|---------|
| Database writes | INSERT, UPDATE, DELETE, MERGE, TRUNCATE |
| Schema changes | ALTER TABLE, DROP COLUMN, CREATE INDEX |
| EF Core migrations | `dotnet ef database update` against any production DB |
| Cache operations | Forced cache invalidation, cache flush commands |
| Service operations | Service restarts, worker process kills |
| Infrastructure | Kubernetes rollouts, ArgoCD syncs, Helm upgrades |
| Cloud resources | AWS EC2/RDS/S3/Lambda modifications, IAM changes |
| Deployments | Any artifact push to a production environment |
| RabbitMQ | Publishing messages to a production exchange or queue |

### 3. Environment Preference Order

Always prefer lower environments. Work in the highest environment that is *necessary*, not the highest that is *available*:

```
Local → Development → Staging → Production
```

Use Production only when the task explicitly requires it and you have received confirmation.

### 4. Production Troubleshooting Approach

When investigating a production issue, exhaust non-invasive options before recommending any operational change:

1. **Code analysis** — trace the logic path in source
2. **Log inspection** — Serilog logs, structured log queries
3. **Metrics** — application performance counters, RabbitMQ queue depths
4. **Read-only queries** — `SELECT` only, `AsNoTracking()`, against a read replica if available

Only after these are exhausted should any write or operational change be considered — and it still requires explicit approval.

### 5. Availability & Integrity Gate

If any requested action could potentially impact Production **availability**, **data integrity**, or **data completeness** — even indirectly — stop and request explicit confirmation before proceeding.

Examples that trigger this gate:
- Running a migration on a table that is actively written to
- Restarting a background service mid-message-processing
- Invalidating a shared cache key that all 7 services depend on
- Publishing a RabbitMQ message that cascades to the OddsEngine

### 6. Connection String & Secret Safety

This codebase has 4 databases and a RabbitMQ broker. Misconfigured connection strings are a silent production risk.

- **Never commit real connection strings, API keys, JWT secrets, or RabbitMQ credentials** to source control.
- `appsettings.json` checked into git must contain only placeholder or non-sensitive values.
- Real credentials must be supplied via `appsettings.Development.json` (git-ignored), environment variables, or a secrets manager.
- Before running any operation that touches external infrastructure (EF migrations, RabbitMQ publish, MongoDB queries), **verify which environment the connection string in use actually targets**.

### 7. RabbitMQ Environment Isolation

Messages published to the OddsEngine exchange (`r.oe.*`) are indistinguishable from production events once they leave the broker. A developer running local code against a production broker will trigger real config changes on the live OddsEngine.

- Confirm the `RabbitMQ:Host` value in `appsettings.json` is non-production before running any code that publishes messages.
- Never use a production RabbitMQ connection string in a local or development environment.
- If a test requires publishing a real message, use the Staging broker and obtain explicit confirmation first.

### 8. EF Core Migration Safety

EF Core migrations run against whichever database the active connection string points to. There is no built-in guard.

- Before running `dotnet ef database update`, always run `dotnet ef migrations list` first and confirm the target database host from the connection string in use.
- Never apply a migration that drops columns or changes data types without a documented rollback plan reviewed before execution.
- Migration commands targeting Production require explicit written approval in the current conversation before execution.

### 9. Multi-Service Deployment Coordination

`Backoffice.Shared` is referenced by all 7 services. Any change to its models, message classes, or DbContext definitions is a cross-service breaking change if deployed to only one service.

- Never deploy a `Backoffice.Shared` change to a single Production service without a coordinated deployment plan for all consumers.
- Document the deployment order and rollback sequence before any Shared change is shipped to Production.

### 10. No Direct Production Database Access via GUI Tools

SQL Server Management Studio, Azure Data Studio, MongoDB Compass, and similar tools bypass all application-layer safety checks and audit logging.

- Never execute a write operation (INSERT, UPDATE, DELETE, schema change) against a Production database via a GUI tool or direct SQL client without explicit approval.
- Even read-only GUI access to Production databases requires confirmation unless already authorised for the current task.
- All production data changes must go through the application's API layer so they are subject to validation, activity logging, and cache invalidation.

### 11. Documentation Requirement for Every Code Change

Every code change — however small — must be traceable. Other developers must be able to understand what changed and why without archaeology.

- Update the relevant `docs/*.md` file(s) if the change affects architecture, messaging flows, caching strategy, or API contracts.
- If a new message type is added → update `docs/06_Messaging.md`.
- If a new project or major component is added → update `docs/02_ProjectOverview.md`.
- If the change introduces or resolves a known technical debt item → update `docs/08_TechnicalDebt.md`.
- Pull request descriptions must state: what changed, which services are affected, and what needs to be tested.

### 12. Focused Commits — One Domain Per Commit

Large, mixed-purpose commits make bugs harder to isolate, rollbacks harder to scope, and reviews harder to reason about.

- Each commit must be focused on a **single domain or concern** (e.g. client margin logic, cache invalidation, translation fallback — not all three at once).
- Large commits that touch multiple unrelated domains are prohibited.
- If a task naturally spans multiple domains, split it into a commit sequence with a clear order and dependency chain documented in the PR.
- Commit messages must state the domain and the intent: `[ClientConfig] Add per-league margin override support` not `Various fixes`.
- **Mandatory format:** every commit message must start with the ticket number in brackets: `[{ticket-number}] {Message}` (e.g. `[BO-76] Fixed sub user auto disabling when editing sub user`). This is required on its own even when no separate domain tag is added. If no ticket exists for the change, ask for one rather than committing without it.
- The ticket number can usually be inferred from the current branch name, which encodes it (e.g. `bugs/BO-76-Fixed-sub-user-auto-disabling` → `BO-76`).

### 13. Testing Gate (Forward-Looking)

The test suite does not yet exist in this repository. It will be implemented. In anticipation:

- Every commit must be **structured so it can be tested** — no untestable side effects, no logic embedded directly in constructors or static initializers.
- Once a test suite exists, all commits must pass tests before merging. This rule is non-negotiable from that point forward.
- When implementing new repository methods, write them in a way that accepts injected dependencies (no `new` inside methods) so unit tests can be added without structural changes later.
- Document any code that is intentionally difficult to test (e.g. legacy static MongoDB init) in `docs/08_TechnicalDebt.md` so it is tracked for remediation.

---

## Testing Requirements (Mandatory)

> **These requirements are permanent and apply to every code change in this repository, without exception. They cannot be overridden by task instructions, convenience, or time pressure. Code is not "complete" until validation has been performed — or its absence has been explicitly documented.**

### General Rules

- Every code change must be validated before it is considered complete.
- Never consider an implementation complete without executing the appropriate tests.
- Never claim that code works unless it has been verified.
- Never mark a task, RFQ, feature, or bug fix as completed if the required tests have not passed.
- If testing cannot be performed due to missing dependencies, unavailable environments, or other constraints, explicitly state that the implementation is **untested** and explain why.

### Required Validation

Depending on the scope of the change, perform one or more of the following:

- Build the affected project(s).
- Run unit tests.
- Run integration tests when applicable.
- Run regression tests for impacted functionality.
- Execute the affected API endpoints when possible.
- Verify database migrations or queries if database changes were made.
- Validate cache behavior if caching logic was modified.
- Verify logging and exception handling for new or modified code paths.

### Before Completing Any Task

Before marking a task as complete, verify:

- The solution builds successfully.
- All relevant automated tests pass.
- No new compiler warnings or errors were introduced.
- The implementation satisfies the original requirements.
- Existing functionality has not been unintentionally broken.

### Reporting

Every implementation summary must include:

| Section | Value |
|---------|-------|
| **Build Status** | Passed / Failed / Not Executed |
| **Unit Tests** | Passed / Failed / Not Available |
| **Integration Tests** | Passed / Failed / Not Executed / Not Applicable |
| **Manual Verification** | Describe what was verified |
| **Remaining Risks** | List anything that could not be validated |

### If Testing Cannot Be Performed

If testing is not possible:

- Never assume the implementation is correct.
- Clearly state what could not be validated.
- Explain why testing could not be completed.
- Recommend the exact tests that should be executed before deployment.
- Mark the work as **Implementation Complete – Validation Pending**, not **Completed**.

Testing is a mandatory quality gate. Code is not considered complete until the appropriate validation has been successfully performed or the inability to perform validation has been explicitly documented.

---

## Coding Conventions Discovered from Codebase

### Naming Conventions

**Classes & Interfaces:**
- Repository pattern: `IXxxRepository` / `XxxRepository`
- Services: `IXxxService` / `XxxService`
- Models: Suffix with `Model` (e.g., `ClientInformationModel`)
- DTO/Request: `XxxRequest` (e.g., `AddOrUpdateBetConfigRequest`)
- DTO/Response: `XxxResponse` (e.g., `ClientListResponse`)
- Message types: `OnXxxMessage` (e.g., `OnDeleteCacheMessage`)
- DbContext: Suffix with `Context` (e.g., `BackofficeContext`)

**Methods:**
- Async methods: Suffix with `Async` (e.g., `GetClientAsync()`)
- Boolean getters: Prefix with `Is` or `Can` (not observed in codebase but recommended)
- Action methods: Use verb-noun pattern (e.g., `AddActivityLog`, `UpdateClientMargin`)

**Properties:**
- PascalCase for all public properties
- Use `#region` blocks for field/property grouping in large classes
- Readonly fields: camelCase prefix with underscore (e.g., `_backofficeContext`)

### Code Organization

```csharp
public class XxxRepository : IXxxRepository
{
    #region Fields
    private readonly DbContext _context;
    private readonly ILogger<XxxRepository> _logger;
    #endregion

    #region Ctor
    public XxxRepository(DbContext context, ILogger<XxxRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    #endregion

    #region Public Methods
    public async Task<BaseResponse<T>> GetXxxAsync() { }
    public async Task<BaseResponse<bool>> AddXxxAsync(XxxModel model) { }
    #endregion

    #region Private Methods
    private async Task ValidateXxxAsync(int id) { }
    #endregion
}
```

### Formatting Standards

- **Indentation:** 4 spaces (not tabs)
- **Line Length:** No strict limit observed; practical limit ~120 characters
- **Braces:** Opening brace on same line (C# style)
- **Using Statements:** Organized alphabetically by namespace
- **Null Coalescing:** Use `??` and `?.` operators
- **String Interpolation:** Prefer `$"text {variable}"` over `string.Format()`

---

## Repository Pattern Rules

### Implementation Requirements

All repositories must implement their corresponding interface:

```csharp
public class XxxRepository : IXxxRepository
{
    // Required implementation
}
```

### Dependency Injection

**Constructor Injection Pattern:**
```csharp
public BackofficeRepository(
    BackofficeContext backofficeContext,
    TranslationContext translationContext,
    IPlatformRepository platformRepository,
    ISportsbookRepository sportsbookRepository,
    ITradingRepository tradingRepository,
    IMemCache memCache,
    IRabbitMqRepository rabbitMqRepository)
{
    _backofficeContext = backofficeContext;
    _translationContext = translationContext;
    // ... store all dependencies
}
```

**Rule:** All repository dependencies are injected in constructor. Never create instances with `new` inside repository methods.

### Return Type Convention

All public repository methods return `BaseResponse<T>` or `BaseResponse`:

```csharp
public async Task<BaseResponse<ClientList>> GetClientsAsync()
{
    try
    {
        var clients = await _context.Clients.ToListAsync();
        return new BaseResponse { Result = clients };
    }
    catch (Exception ex)
    {
        _logger.LogError("Error: {Exception}", ex);
        return new BaseResponse { StatusCode = StatusCode.Error };
    }
}
```

**Rule:** Every public method wraps its return value in `BaseResponse`. Exceptions are caught and logged, never thrown upward.

### Database Context Management

**Multiple Contexts Per Repository:**
```csharp
// BackofficeRepository uses 2 contexts:
private readonly BackofficeContext _backofficeContext;
private readonly TranslationContext _translationContext;

// When saving across contexts:
using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    await _backofficeContext.SaveChangesAsync();
    await _translationContext.SaveChangesAsync();
    transaction.Complete();
}
```

**Rule:** When a single operation spans multiple databases, wrap in `TransactionScope` with `TransactionScopeAsyncFlowOption.Enabled`.

### No Transaction Management in Background Services

Background workers (Config.Service, Utility, WebSocket.Service) **must not** use:
- `TransactionScope`
- Manual transaction management

**Reason:** Background services are fire-and-forget; they don't report success/failure back to clients.

---

## Service Layer Conventions

### Service vs. Repository Distinction

- **Repository:** Data access abstraction; implements IRepository
- **Service:** Business logic; uses one or more repositories
- **Current Codebase:** Repository layer is conflated with service layer

### When Service Layer is Used

Services are explicit in:
- **Backoffice.Web** (Blazor component logic)
- **Background Services** (Event processing logic)

Example from Backoffice.Web:
```csharp
public class ClientService
{
    private readonly HttpClient _httpClient;  // Calls API
    private readonly ILocalStorageService _localStorage;

    public async Task<ClientList> GetClientsAsync()
    {
        // Calls API, caches locally
    }
}
```

### Rules for Service Implementation

1. Services call repositories or external APIs
2. Services implement business rules and orchestration
3. Services handle caching logic
4. Services do NOT handle database operations directly
5. Always use dependency injection; never instantiate repositories/services with `new`

---

## MongoDB Conventions

### Connection Model

MongoDB has a **static, lazy-initialized connection**:

```csharp
private static IMongoDatabase _db;  // Shared across all instances

public SportsbookRepository()
{
    if (_db == null)
    {
        var client = new MongoClient(connectionString);
        _db = client.GetDatabase("Sportsbook");
    }
}
```

**Current Issue:** Not thread-safe. Race conditions possible (documented in 13_RiskyImplementations.md).

### Read Preference

MongoDB collections use `PrimaryPreferred`:

```csharp
var options = new FindOptions { ReadPreference = ReadPreference.PrimaryPreferred };
var collection = _db.GetCollection<T>("CollectionName");
var results = await collection.FindAsync(filter, options);
```

**Rule:** Always specify `ReadPreference.PrimaryPreferred` to allow reading from replicas when primary is busy.

### Caching Strategy

MongoDB is primarily a **cache layer** for Sportsbook data:

1. SQL Server = source of truth for fixtures/markets
2. MongoDB = denormalized, faster-access cache
3. Cache invalidation via RabbitMQ messages

### Collections

```
Database: Sportsbook

Collections:
- Fixtures (fixture/market/selection data)
- Markets
- Selections
- Sports (lookup)
- Leagues (lookup)
- Segments (hierarchical)
```

**Rule:** MongoDB collections are indexed for lookups. Never query without index consideration.

---

## SQL Conventions

### Four-Database Architecture

```
1. BackofficeContext (Backoffice database)
   └─ ClientInformation, ClientBetSetting, ConfigTemplate, ConfigTemplateMargin, etc.

2. TradingContext (Trading database)
   └─ Sport/League/Market/Selection mappings

3. SportsbookContext (Sportsbook database)
   └─ Fixture/Market/Selection data

4. TranslationContext (Translation database)
   └─ Translation mappings for all languages
```

### DbContext Lifecycle

- **Backoffice.API:** Scoped per HTTP request
- **Background Services:** Created per message processing
- **Query Timeout:** 180 seconds (configured in appsettings)

### Entity Framework Usage

**Pattern Observed:**
```csharp
var clients = await _context.Clients
    .AsNoTracking()  // Important for read-heavy queries
    .Where(c => c.Status == ClientStatus.Active)
    .ToListAsync();
```

**Rules:**
1. Use `AsNoTracking()` for read-only queries (no change tracking overhead)
2. Use `ToListAsync()` for async operations; never `.ToList()`
3. Always use `.Include()` for navigation properties before using them
4. Use `DbContextPool` for connection pooling (configured in Startup.cs)

### Complex Queries

The BackofficeRepository contains complex queries with:
- Composite keys
- Denormalized data
- Activity log queries joining multiple tables
- Market configuration hierarchies

**Rule:** Keep complex queries in repository methods; don't scatter query logic across services.

### No Manual SQL

Use Entity Framework exclusively. Manual SQL strings are forbidden:
- Reduces SQL injection risk
- Enables database portability
- Helps with migration

**Exception:** Stored procedures may be used for extreme performance-critical paths (not currently used).

---

## Caching Rules

### Caching Architecture

```
Layer 1: Browser cache (HTTP headers)
Layer 2: Application MemoryCache (in-process)
Layer 3: MongoDB cache (distributed)
Layer 4: SQL Server (source of truth)
```

### IMemCache Interface

```csharp
public interface IMemCache
{
    // Add/update with TTL in hours
    void AddOrUpdateCache<T>(string key, T cacheObject, int cacheTimeout);
    
    // Add/update with absolute expiration
    void AddOrUpdateCache<T>(string key, T cacheObject, DateTimeOffset date);
    
    // Get with lazy-load fallback
    T GetCache<T>(string key, Func<T> repositoryCall, int expiration);
    
    // Simple get
    T GetCache<T>(string key);
    
    // Get with fallback value
    KeyValuePair<bool, long> GetCache(string key);
    
    // Remove entry
    void DeleteCache(string key);
}
```

### Caching Patterns

**Pattern 1: Lazy Load with Auto-Populate**
```csharp
var cached = _memCache.GetCache<ClientList>(
    "clients.all",
    () => _context.Clients.AsNoTracking().ToList(),
    expiration: 1  // 1 hour
);
```

**Pattern 2: Explicit Set**
```csharp
_memCache.AddOrUpdateCache(
    "config.template.123",
    configData,
    cacheTimeout: 24  // 24 hours
);
```

**Pattern 3: Invalidation**
```csharp
_memCache.DeleteCache("clients.all");
// Broadcast to other services
await _messageBroker.Publish(new OnDeleteCacheMessage
{
    CacheKey = "clients.all",
    ClientId = 123
});
```

### Cache Key Naming Convention

```
{entity}.{operation}.{identifier}

Examples:
- "clients.all"
- "config.template.123"
- "config.margin.abc_def"
- "cc:{clientId}"  // Short form used in RabbitMQ
```

**Rule:** Use consistent, hierarchical naming for cache keys. Avoid cryptic abbreviations.

### Cache Invalidation

All cache invalidation must be:
1. Logged to activity log
2. Published via RabbitMQ with `OnDeleteCacheMessage`
3. Handled by all consumer services (Utility, WebSocket.Service)

**Rule:** Never invalidate cache locally only. Always broadcast to other services.

---

## Translation Rules

### Translation Architecture

Translation data is stored in **TranslationContext** database:

```csharp
private readonly TranslationContext _translationContext;

// Entities with translations:
- FixtureTranslation
- MarketTranslation
- LeagueTranslation
- SportTranslation
- SelectionTranslation
- SegmentTranslation
```

### Language Support

Languages are identified by:
- ISO 639-1 codes (e.g., "en", "es", "fr")
- Region variants (e.g., "en-US", "pt-BR")

### Translation Retrieval Pattern

```csharp
var translations = await _translationContext.FixtureTranslations
    .Where(t => t.FixtureId == fixtureId && t.LanguageCode == languageCode)
    .ToListAsync();
```

### Fallback Logic

When translation is missing for a language:
1. Try requested language
2. Fall back to language base (e.g., "en" if "en-US" missing)
3. Fall back to default language (usually English)
4. Fall back to entity name/code

**Rule:** Never return null for translations. Always provide fallback value.

---

## Messaging/Event Handling Rules

### Message Broker: RabbitMQ

**Configuration:**
- Host: Read from appsettings
- Topic Exchange pattern: Region-based routing
- Connection: HTTP-based management API calls

### Message Publishing Pattern

```csharp
await _rabbitMqRepository.Publish(new MessageRequest
{
    Payload = JsonSerializer.Serialize(dataObject),
    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
    MessageType = "UpdateClientMarketConfig",
    RoutingKey = "r.oe.cachesync.onmessagetype"
}, region: "HK");  // Region affects routing
```

### Current Issue: Fire-and-Forget

Messages are published without awaiting or checking results:

```csharp
_ = _rabbitMqRepository.Publish(...);  // Result ignored
```

**Consequence:** Publishing failures are not detected. This is a known limitation (documented in 12_PerformanceBottlenecks.md).

### Message Type Routing

Outgoing message types:
- **Configuration:** `OnUpdateClientMarketConfigMessage`, `OnUpdateClientLeagueConfigMessage`
- **Cache Invalidation:** `OnDeleteCacheMessage`
- **Activity Logging:** `OnActivityLogMessage`, `OnActivityLogListMessage`
- **Exports:** `OnGetManualBetSettlementHistoryMessage`, etc.

**Routing Rule:** Routing key format is `r.oe.{topic}.{messagetype}` (lowercase).

### Consumer Pattern (Background Services)

```csharp
switch (messageType)
{
    case "UpdateClientMargin":
        var margin = JsonSerializer.Deserialize<long>(payload);
        await _repository.UpdateClientMarginAsync(margin);
        break;
        
    case "DeleteConfigTemplate":
        var template = JsonSerializer.Deserialize<ConfigTemplate>(payload);
        await _repository.DeleteConfigTemplateAsync(template);
        break;
}
```

**Rule:** Consumers must handle all expected message types. Add new types with caution; ensure all producing services are updated.

### Message Serialization

Use `System.Text.Json.JsonSerializer`:

```csharp
var payload = JsonSerializer.Serialize(dataObject);
var deserialized = JsonSerializer.Deserialize<T>(payload);
```

**Note:** Backoffice.API also has legacy `Newtonsoft.Json` usage. New code should use `System.Text.Json`.

---

## API Implementation Guidelines

### REST Endpoint Pattern

All endpoints follow this structure:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Requires JWT token
public class ClientController : ControllerBase
{
    private readonly IClientRepository _repository;

    [HttpGet("{clientId}")]
    public async Task<IActionResult> GetClient(int clientId)
    {
        var response = await _repository.GetClientAsync(clientId);
        return Ok(response);  // Always return BaseResponse<T>
    }

    [HttpPost]
    [ApiKey]  // Requires x-api-key header
    public async Task<IActionResult> CreateClient(CreateClientRequest request)
    {
        var response = await _repository.CreateClientAsync(request);
        return Ok(response);
    }
}
```

### Request/Response Envelope

```csharp
public class BaseResponse<T>
{
    public T Result { get; set; }
    public StatusCode StatusCode { get; set; }
    public string Message { get; set; }
}

public enum StatusCode
{
    Success = 1,
    Error = 2,
    ValidationError = 3,
    NotFound = 4,
    Unauthorized = 5
}
```

**Rule:** All API responses use this envelope. Never return raw data directly.

### HTTP Status Codes

```
200 OK          → BaseResponse with StatusCode.Success
400 Bad Request → BaseResponse with StatusCode.ValidationError
401 Unauthorized→ BaseResponse with StatusCode.Unauthorized
404 Not Found   → BaseResponse with StatusCode.NotFound
500 Server Error→ BaseResponse with StatusCode.Error (logged)
```

### Validation

Request validation happens at:
1. **Model Level:** Data annotations on request classes
2. **Repository Level:** Business rule validation

```csharp
[Required]
[StringLength(100)]
public string ClientName { get; set; }

[Range(1, int.MaxValue)]
public int MinimumBet { get; set; }
```

### Error Responses

Errors should never expose:
- Stack traces to clients
- Internal database structure
- Configuration details
- Stack traces are only logged server-side

---

## Logging Conventions

### Logging Framework

**Framework:** Serilog 4.3.0  
**Configuration:** Centralized in appsettings.json via `Serilog:` section

### Logging Pattern

```csharp
private readonly ILogger<XxxRepository> _logger;

try
{
    // Operation
}
catch (Exception ex)
{
    _logger.LogError("Failed to update client {ClientId}: {Exception}", clientId, ex);
    return new BaseResponse { StatusCode = StatusCode.Error };
}
```

### Log Levels

- **LogError:** Exceptions, caught errors, failures
- **LogWarning:** Unusual conditions, deprecated usage
- **LogInformation:** Important state changes (client created, config updated)
- **LogDebug:** Detailed diagnostic info (method entry/exit, parameter values)

**Rule:** Use structured logging with named parameters: `_logger.LogError("Error: {ParamName}", paramValue)`.

### Sensitive Data

Never log:
- Passwords
- API keys
- Personal user information
- Raw SQL queries

---

## Error Handling Conventions

### Global Error Pattern

All repositories follow this pattern:

```csharp
public async Task<BaseResponse<T>> GetXxxAsync()
{
    try
    {
        var result = await _context.Xxx
            .AsNoTracking()
            .FirstOrDefaultAsync();
        
        return new BaseResponse { Result = result };
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to get Xxx: {Exception}", ex);
        return new BaseResponse { StatusCode = StatusCode.Error };
    }
}
```

### Error Response Handling

Error responses must include:
1. Clear error message (if appropriate)
2. Status code indicating error type
3. Logged exception details (server-side only)

### No Global Exception Handler

The codebase does NOT have a global exception middleware. Exceptions are caught in repositories.

**Note:** This is documented as a technical debt item (see 08_TechnicalDebt.md). When adding new services, consider adding global exception handling if appropriate.

### Database-Specific Errors

```csharp
catch (DbUpdateException dbEx)
{
    _logger.LogError("Database update failed: {Exception}", dbEx.InnerException);
    return new BaseResponse { StatusCode = StatusCode.Error };
}
catch (SqlException sqlEx)
{
    _logger.LogError("SQL error occurred: {SqlException}", sqlEx);
    return new BaseResponse { StatusCode = StatusCode.Error };
}
```

---

## Performance Expectations

### Known Performance Issues

1. **HttpClient Creation:** Clients created per request (should be singleton/pooled)
2. **N+1 Queries:** No explicit query optimization observed
3. **In-Process Cache:** Not shared across server instances
4. **MongoDB Queries:** No explicit indexing strategy
5. **Message Serialization:** JSON serialization for every message

### Performance Guidelines

When implementing new features:

1. **Database Queries:**
   - Use `AsNoTracking()` for read-only queries
   - Specify `.Include()` for needed relationships
   - Avoid querying without filtering (WHERE clause)

2. **Caching:**
   - Cache expensive database queries
   - Use appropriate TTL (1-24 hours depending on volatility)
   - Invalidate cache when data changes

3. **External API Calls:**
   - Use HttpClient pool (should be injected, not created)
   - Add timeout (30s recommended)
   - Cache responses when appropriate

4. **Message Processing:**
   - Batch messages when possible
   - Avoid blocking operations in message handlers
   - Consider message compression for large payloads

### Query Timeout

All database operations have **180-second timeout** configured in Startup.cs. Don't exceed this.

---

## Feature Implementation Workflow

### Step 1: Define the Feature

- Identify which project(s) are affected
- Determine if new database table is needed
- Plan message flow if async component involved

### Step 2: Data Layer

1. Add entity to appropriate DbContext if needed
2. Implement repository method following convention
3. Add database migration (if not auto-applied)
4. Test directly against database

### Step 3: Service Layer

1. Create service class following naming conventions
2. Inject repository dependencies
3. Implement business logic
4. Add caching if needed

### Step 4: API Layer

1. Add controller or extend existing
2. Create request/response DTOs
3. Implement endpoint with proper authorization
4. Add Swagger documentation

### Step 5: WebSocket/Real-Time (if needed)

1. Add SignalR Hub method if real-time updates needed
2. Add message type to OutgoingEngineMessages
3. Update WebSocket.Service message handler

### Step 6: Background Processing (if needed)

1. Define message type in Shared
2. Add publishing in API layer
3. Add consuming logic in appropriate background service
4. Add activity logging

### Step 7: Testing

1. Test API endpoint manually
2. Verify message publishing/consumption
3. Check cache invalidation
4. Verify cross-service integration

### Step 8: Documentation

1. Update 02_ProjectOverview.md if adding new project
2. Update 06_Messaging.md if adding message type
3. Add code comments for complex logic

---

## Bug Fixing Workflow

### Step 1: Reproduce

1. Identify which service exhibits the bug
2. Determine if it's API, background service, or WebSocket
3. Create minimal reproducible test case
4. Understand root cause

### Step 2: Identify Affected Services

Check if bug touches:
- Database operations → May need migration
- Message handling → May need message handler update
- Cache operations → May need cache invalidation
- External API calls → May need fallback logic

### Step 3: Fix Implementation

1. Implement minimal fix in responsible repository/service
2. Add error handling if not present
3. Add logging for troubleshooting
4. Verify backward compatibility

### Step 4: Cascade Testing

1. Test in originating service
2. Test in consuming services
3. Verify cache invalidation propagates
4. Verify message handling works correctly

### Step 5: Deployment Considerations

1. Does fix require database migration?
2. Does fix require configuration change?
3. Is backwards compatibility maintained?
4. Are there dependent services to update?

---

## Code Review Checklist

Before submitting code for review, verify:

### Architecture & Design
- [ ] Follows established repository pattern
- [ ] Uses dependency injection (no `new` keyword for services)
- [ ] Maintains single responsibility
- [ ] Backward compatible (no breaking API changes)

### Database & Performance
- [ ] Uses `AsNoTracking()` for read queries
- [ ] Includes `.Include()` for navigation properties
- [ ] Uses appropriate caching (or explains why not)
- [ ] No N+1 query patterns observed
- [ ] Query timeout respected (180s)

### Error Handling & Logging
- [ ] All repository methods wrapped in try-catch
- [ ] Returns `BaseResponse` with appropriate `StatusCode`
- [ ] Meaningful error logged with context
- [ ] No sensitive data logged

### Messaging & Events
- [ ] Message types defined in Backoffice.Shared
- [ ] Publishing includes `CorrelationId`
- [ ] Consumer handles all expected message types
- [ ] Cache invalidation cascaded via RabbitMQ

### Security & Validation
- [ ] API endpoints have appropriate `[Authorize]` or `[ApiKey]` filters
- [ ] Request models validated with data annotations
- [ ] No SQL injection vectors (EF Core usage)
- [ ] API key and JWT logic preserved

### Documentation & Testing
- [ ] Complex logic commented
- [ ] New message types documented
- [ ] Tested across all affected services
- [ ] No compiler warnings

---

## Rules for Using the Reference Folder

The codebase doesn't explicitly define a "Reference" folder, but there is a **Shared.Models** containing reference/utility data:

```
Backoffice.Shared/Models/
├─ Enums/              → Reference enumerations
├─ Response/           → Shared response models
├─ Requests/           → Shared request models
└─ OutgoingEngineMessages/  → Message definitions
```

### Usage Rules

1. **When to Create Shared Models:**
   - Data crossed by multiple projects
   - Message definitions
   - Common enumerations
   - Common request/response types

2. **When NOT to Create Shared Models:**
   - Project-specific entities (use in project's own Models folder)
   - Internal implementation details
   - Service-specific DTOs

3. **Naming in Shared:**
   - Keep names clear and non-ambiguous
   - Prefix with entity type: `ClientXxx`, `ConfigXxx`
   - Keep backward compatibility strict

---

## Rules for Minimizing Code Changes

### Philosophy

Production systems must be changed minimally and carefully. Each change carries deployment risk.

### Rules

1. **Don't Refactor Unrelated Code**
   - Fix only the identified bug/feature
   - Don't reorganize files or methods
   - Don't rename classes/properties for aesthetics

2. **Preserve API Contracts**
   - Never remove parameters from API endpoints
   - Never change parameter types
   - Never change response structure
   - Add new fields to responses (optional), don't remove

3. **Use Feature Flags for Risky Changes**
   - Feature enabled via appsettings
   - Can be toggled without redeployment
   - Allows gradual rollout

4. **Extend, Don't Replace**
   - Add new repository methods, don't delete old ones
   - Add new message types, don't remove old ones
   - Add new configuration options, don't remove existing

5. **Test Downstream Impact**
   - Changed repository method? Test all calling code
   - Changed message format? Test all consumers
   - Changed database? Test all accessing services

---

## Rules for Preserving Backward Compatibility

### API Compatibility

**Breaking Changes (NEVER do):**
```csharp
// BAD: Removes parameter
public async Task<BaseResponse<T>> GetClient(int clientId)  // Was (int clientId, string region)

// BAD: Changes response type
public async Task<Client> GetClient()  // Was BaseResponse<Client>

// BAD: Removes response field
return new BaseResponse { Result = client };  // Was { Result, Metadata, Timestamp }
```

**Safe Changes:**
```csharp
// GOOD: Adds optional parameter
public async Task<BaseResponse<T>> GetClient(int clientId, string region = "default")

// GOOD: Adds new response field
return new BaseResponse 
{ 
    Result = client,
    Metadata = new { },  // NEW field
    Timestamp = DateTime.UtcNow  // NEW field
};
```

### Message Compatibility

**Breaking Changes (NEVER do):**
```csharp
// BAD: Message type disappears
case "GetManualBetSettlementHistory":  // REMOVED

// BAD: Required field removed
public class OnDeleteCacheMessage
{
    public string CacheKey { get; set; }  // REMOVED: ClientId
}
```

**Safe Changes:**
```csharp
// GOOD: Add new message type
case "GetClientTransactionHistory":  // NEW

// GOOD: Add optional field
public class OnDeleteCacheMessage
{
    public string CacheKey { get; set; }
    public int? ClientId { get; set; }  // NEW: optional
}
```

### Database Compatibility

**Safe Migration Path:**
```
Step 1: Add new column (nullable)
Step 2: Deploy code that populates new column
Step 3: Deploy code that uses new column
Step 4: (Later) Remove old column if not needed
```

**Never:**
- Drop columns without migration period
- Change data types without migration
- Remove tables immediately

### Configuration Compatibility

Always provide sensible defaults:

```csharp
public class AppSettings
{
    public string CacheTimeout { get; set; } = "1h";  // Default
    public int HttpTimeout { get; set; } = 30;  // Default seconds
}
```

---

## Project Structure Overview

```
d:\Gimli\Repositories\backoffice-final\

├── Backoffice.API/
│   ├── ActionFilter/
│   │   ├── ApiKeyFilter.cs
│   │   └── AuthorizeAttribute.cs
│   ├── Controllers/           (REST endpoints)
│   ├── Repositories/          (Data access & business logic)
│   ├── Startup.cs
│   └── Program.cs
│
├── Backoffice.Web/
│   ├── Components/            (Blazor components)
│   ├── Services/              (Service layer)
│   ├── Pages/
│   └── Program.cs
│
├── Backoffice.WebSocket.API/
│   ├── Hubs/                  (SignalR hubs)
│   └── Events/
│
├── Backoffice.Config.Service/
│   ├── Worker.cs              (Message consumer)
│   └── Repository/
│
├── Backoffice.Utility/
│   ├── Worker.cs              (Message consumer)
│   └── Repository/
│
├── Backoffice.WebSocket.Service/
│   ├── Worker.cs              (Message consumer)
│   └── Services/
│
├── Backoffice.Shared/
│   ├── Data/                  (DbContexts: Backoffice, Trading, Sportsbook, Translation)
│   ├── Models/                (Request/Response/Message DTOs)
│   ├── Enums/
│   ├── Extensions/
│   ├── Utilities/
│   └── OutgoingEngineMessages/
│
└── docs/                      (Architecture documentation)
    ├── 00_INDEX.md
    ├── 01_Architecture.md
    ├── 02_ProjectOverview.md
    ... (12 more documents)
```

---

## Key Files to Reference

| File | Purpose | When to Check |
|------|---------|---------------|
| `Startup.cs` (all projects) | DI configuration | Adding new services/repositories |
| `Backoffice.Shared/Data/` | Entity definitions | Adding new tables/contexts |
| `appsettings.json` | Configuration | Understanding runtime behavior |
| `docs/01_Architecture.md` | Architecture details | Understanding system design |
| `docs/06_Messaging.md` | Message types | Adding message publishing/consuming |
| `Repositories/IXxxRepository` | API contracts | Understanding repository interface |

---

## Common Patterns & Anti-Patterns

### Pattern: Repository Method Signature

```csharp
// ✓ GOOD: Async, returns BaseResponse
public async Task<BaseResponse<ClientList>> GetClientsAsync()
{
    try { }
    catch { }
}

// ✗ BAD: Synchronous, no BaseResponse wrapper
public ClientList GetClients()
{
    // Direct return
}
```

### Pattern: Service Dependency Injection

```csharp
// ✓ GOOD: Constructor injection
public class XxxService
{
    private readonly IXxxRepository _repository;
    
    public XxxService(IXxxRepository repository)
    {
        _repository = repository;
    }
}

// ✗ BAD: Creating instance with new
public class XxxService
{
    public void DoWork()
    {
        var repo = new XxxRepository();  // ✗ Not injectable
    }
}
```

### Pattern: Error Handling

```csharp
// ✓ GOOD: Caught, logged, returned in BaseResponse
try
{
    await _context.SaveChangesAsync();
    return new BaseResponse { StatusCode = StatusCode.Success };
}
catch (Exception ex)
{
    _logger.LogError("Error: {Exception}", ex);
    return new BaseResponse { StatusCode = StatusCode.Error };
}

// ✗ BAD: Exception thrown, service crashes
try
{
    await _context.SaveChangesAsync();
}
catch
{
    throw;  // Propagates to client
}
```

### Pattern: Message Publishing

```csharp
// ✓ GOOD: Region-specific, with CorrelationId
await _messageBroker.Publish(new OnDeleteCacheMessage
{
    CacheKey = key,
    ClientId = clientId
}, region: "HK");

// ✗ BAD: Fire-and-forget, no error handling
_ = _messageBroker.Publish(message);  // Result ignored
```

---

## Critical Constraints (DO NOT IGNORE)

1. **.NET Core 3.1 End-of-Life:** Framework version is obsolete. Impacts library choices.
2. **Single-Instance WebSocket:** `BackofficeHub` uses static dictionary. Not distributed.
3. **No Idempotency:** Message processing not idempotent. Duplicate messages cause duplicate processing.
4. **FireAndForget Messages:** Publishing failures are not detected or retried.
5. **In-Process Cache:** Cache not shared across server instances.
6. **Monolithic Repositories:** 2000+ line repositories are hard to test and maintain.

---

## Questions to Ask When Modifying Code

1. **Does this change affect multiple projects?** If yes, plan testing across all.
2. **Is a database migration needed?** If yes, coordinate with deployment.
3. **Will this message be consumed by other services?** If yes, update all consumers.
4. **Are there caching implications?** If yes, ensure cache invalidation propagates.
5. **Does this break existing API contracts?** If yes, reconsider approach.
6. **Are there performance implications?** If yes, benchmark against current behavior.
7. **Is backward compatibility maintained?** If no, coordinate deprecation period.

---

## Sibling Service Repositories

All related service repos are **siblings** of `backoffice-final` in the same parent directory. Use relative paths (`../oddsfeed/`, `../platform-final/`, etc.) — never hardcode absolute paths, as other developers may have a different root.

| Repo | Contents |
|------|----------|
| `../Auth/` | Auth.Api (JWT issuance) |
| `../odds-engine-2/` | OddsEngine v2 (current): OE.Adapter.Service, OE.Sportsbook.Service, OE.UOF.Client, OE.Job.Settlement |
| `../oddsengine/` | OddsEngine v1 (old, reference baseline for odds-engine-2 only — not active) |
| `../oddsfeed/` | OF.Settlement.Service (primary bet settlement + ResettleBet), OF.Operator.Service, OF.Placement.Service |
| `../platform-final/` | Platform.Settlement.Service, Platform.Operator.Servicev2, Platform.Sportsbook.API, Platform.BetForwarder.Service, Platform.CashOutStake.Servicev2, Platform.Shared |
| `../platform-web/` | Platform web frontend |
| `../bet-genius/` | BetGenius integration |
| `../webprops-api/` | Web Properties API (Go) — per-client feature flags/themes/layouts + maintenance flags, S3-backed. Consumed via `IWebPropsRepository` (`Backoffice.Web` "Feature Flags" page) |
| `../argocd/` | Kubernetes / ArgoCD deployment configs |
| `../Reference/` | **Frozen AI baseline snapshot. Do not use for cross-service comparison — use the live sibling repos instead.** |

**Full doc:** See `docs/15_ReferenceRepositories.md` in this repo.

---

## Observability (Grafana Staging)

- **URL:** https://grafana.stg.song88.com/
- **Authentication:** Google OAuth — log in with your `@song88.com` Google account (interactive browser flow).
- **Data Sources:**
  - **Loki** — application logs
  - **Prometheus** — metrics
- **Programmatic access:** The OAuth browser flow does not work for API calls. To query Grafana's REST API, generate a service account token / API key from Grafana's Administration panel. Without a token, REST API queries will fail.

---

## BO API Access (Local / Ad-hoc Debugging)

To authenticate to the deployed **BO API** for read-only investigation (e.g. replay an endpoint as `Backoffice.Web` does):

- **Credentials:** git-ignored `.env.bo-api.local` at the repo root (Staging only). If present, read it to log in without prompting the user. Never commit it; never put Production credentials in it.
- **Full procedure & curl recipe:** `docs/16_BoApiAccess.md`.
- **In short:** most endpoints need **two** headers — `x-api-key` (`Security:ApiKey`) **and** `Authorization: Bearer <JWT>`. Get the JWT via `POST {BO_API_BASE}/api/authentication/login` with the username/password from the env file, then call the target endpoint with both headers.
- **Staging only** unless Production is explicitly authorised in the current conversation.

---

## Related Documentation

- [Architecture Analysis](./docs/01_Architecture.md) - Detailed system design
- [Project Overview](./docs/02_ProjectOverview.md) - All 7 projects described
- [Messaging Architecture](./docs/06_Messaging.md) - RabbitMQ message flows
- [Caching Strategy](./docs/07_Caching.md) - In-process and distributed caching
- [Technical Debt](./docs/08_TechnicalDebt.md) - Known issues to address
- [Code Smells](./docs/11_CodeSmells.md) - SOLID principle violations
- [Risky Implementations](./docs/13_RiskyImplementations.md) - Concurrency and thread-safety issues

---

## Quick Reference Commands

```powershell
# Run API locally
dotnet run --project Backoffice.API

# Run background service
dotnet run --project Backoffice.Config.Service

# Run tests (if configured)
dotnet test

# Build solution
dotnet build

# Database migrations (if using EF Core)
dotnet ef migrations add MigrationName --project Backoffice.API
dotnet ef database update --project Backoffice.API
```

---

**Document Maintenance:** Update this file when adding new projects, changing architecture, or discovering new patterns. Keep it aligned with actual codebase conventions.
