# Code Smells

**Document Status:** Analysis Complete (No Code Changes Made)  
**Date:** June 18, 2026

---

## SOLID Principle Violations

### Code Smell #1: Single Responsibility Principle (SRP) Violation

**Location:** `Backoffice.API/Repositories/BackofficeRepository.cs`

**Metrics:**
- 2000+ lines of code
- 50+ public methods
- 8+ different entity types managed
- Multiple concerns mixed

**Responsibilities:**
```
1. Client management (CRUD, queries, updates)
2. Configuration templates (CRUD, assignments, margins)
3. Activity logging (record, query)
4. Client settings (update, apply)
5. Market configuration (create, delete, bulk)
6. Translation templates
7. Database state management
8. Error handling
```

**Issue:**
```csharp
public class BackofficeRepository : IBackofficeRepository
{
    // Client operations
    public async Task<ClientInformation> GetClientAsync(int clientId) { }
    
    // Config operations
    public async Task<ConfigTemplate> GetConfigTemplateAsync(int templateId) { }
    
    // Activity logging
    public async Task<BaseResponse<bool>> AddActivityLogAsync(...) { }
    
    // ... 50+ more methods
}
```

**Violation:**
- Single class handles 8+ different entity types
- Should split into: ClientService, ConfigService, ActivityService, etc.
- Hard to test (mocking all dependencies)
- Hard to maintain (find the method among 50+)
- High coupling

---

### Code Smell #2: Open/Closed Principle (OCP) Violation

**Location:** `Backoffice.Config.Service/Worker.cs`

**Code:**
```csharp
switch (messageType)
{
    case "UpdateClientMargin":
        ProcessUpdateClientMargin(message);
        break;
        
    case "DeleteConfigTemplate":
        ProcessDeleteConfigTemplate(message);
        break;
        
    // To add new message type:
    // Must modify Worker.cs, add new case
    // Violates OCP: Open for extension, Closed for modification
}
```

**Better Pattern:**
```csharp
// Handler registry (Strategy pattern)
var handlers = new Dictionary<string, IMessageHandler>
{
    { "UpdateClientMargin", new UpdateClientMarginHandler() },
    { "DeleteConfigTemplate", new DeleteConfigTemplateHandler() }
};

// To add new type: just register new handler
// No modification to Worker.cs
var handler = handlers[messageType];
await handler.HandleAsync(message);
```

---

### Code Smell #3: Dependency Inversion Principle (DIP) Violation

**Location:** `Backoffice.API/Repositories/RabbitMQRepository.cs`

**Code:**
```csharp
public RabbitMQRepository(IConfiguration configuration)
{
    // Creates HttpClient directly (dependency)
    var client = new HttpClient();  // Concrete dependency
    
    // Creates ConnectionFactory directly
    var factory = new ConnectionFactory();  // Concrete dependency
    
    // Should inject these!
}
```

**Issue:**
- Depends on concrete implementations
- Hard to test (can't mock HttpClient)
- Hard to swap implementations

---

## Code Duplication

### Code Smell #4: Repeated Error Handling

**Location:** Multiple repositories

**Pattern 1 (BackOfficeRepository.cs):**
```csharp
try
{
    await _context.SaveChangesAsync();
    return new BaseResponse { StatusCode = StatusCode.Success };
}
catch (Exception ex)
{
    _logger.LogError("Error: {ex}", ex);
    return new BaseResponse { StatusCode = StatusCode.Error };
}
```

**Pattern 2 (TradingRepository.cs):**
```csharp
try
{
    await _context.SaveChangesAsync();
    return new BaseResponse { StatusCode = StatusCode.Success };
}
catch (Exception ex)
{
    return new BaseResponse { StatusCode = StatusCode.Error };
}
```

**Pattern 3 (SportsbookRepository.cs):**
```csharp
try
{
    // Database operation
}
catch (DbUpdateException ex)
{
    _logger.LogError("Db error: {ex}", ex.InnerException);
    return new BaseResponse { StatusCode = StatusCode.Error };
}
```

**Issue:**
- Same try-catch pattern in 20+ places
- Inconsistent error logging
- Should extract to base class or utility method

---

### Code Smell #5: Repeated HttpClient Setup

**Location:** Multiple repositories

**RabbitMQRepository.cs:**
```csharp
private HttpClient GetHttpClient(string region)
{
    var client = new HttpClient();
    client.BaseAddress = new Uri(uri);
    client.DefaultRequestHeaders.Add("x-api-key", apiKey);
    client.DefaultRequestHeaders.Add("Content-Type", "application/json");
    return client;
}
```

**PlatformRepository.cs:**
```csharp
var client = new HttpClient();
client.BaseAddress = new Uri(platformUrl);
client.DefaultRequestHeaders.Add("x-api-key", apiKey);
client.DefaultRequestHeaders.Add("Content-Type", "application/json");
```

**Issue:**
- Same setup in multiple places
- HttpClient created incorrectly (should be reused)
- Should use HttpClientFactory

---

### Code Smell #6: Repeated Configuration Access

**Location:** All repositories

**Pattern:**
```csharp
var connectionString = _configuration["ConnectionStrings:Backoffice"];
var apiKey = _configuration["API:Trading:APIKey"];
var uri = _configuration["API:Trading:Uri"];
```

**Issue:**
- String keys scattered throughout
- Easy to mistype (typo = runtime error)
- No centralization
- Should use IOptions<T>

---

### Code Smell #7: Repeated Cache Key Building

**Location:** Multiple places

**Pattern 1:**
```csharp
var cacheKey = $"client.{clientId}.config";
_memCache.AddOrUpdateCache(cacheKey, data, 1);
```

**Pattern 2:**
```csharp
var key = "client." + clientId + ".config";
_memCache.GetCache<Config>(key);
```

**Issue:**
- Keys inconsistently formatted
- Easy to mismatch
- Should centralize cache key generation

---

## Large Classes

### Code Smell #8: Monolithic Classes

| Class | Lines | Methods | Responsibilities |
|-------|-------|---------|------------------|
| BackofficeRepository | 2000+ | 50+ | 8+ (Client, Config, Activity, Settings, etc.) |
| RabbitMQRepository | 500+ | 15+ | Publishing, configuration, region handling |
| PlatformRepository | 400+ | 12+ | API calls, error handling |
| SportsbookRepository | 350+ | 10+ | SQL + MongoDB operations |

**Issue:**
- Hard to understand (which method does what?)
- Hard to test (mock everything)
- Hard to debug (find the bug among 2000 lines)
- High maintenance burden
- Poor reusability

**Solution:**
- Split into service layer
- Create focused repositories (one entity type each)
- Extract common logic to utilities

---

## Unclear Code

### Code Smell #9: Ambiguous Method Names

**Location:** Various repositories

**Examples:**
```csharp
// What does this do? Get or set?
public async Task<BaseResponse> ProcessActivity(ActivityModel model) { }

// Get what? All? For a client? For a user?
public async Task<List<LogEntry>> GetLogs() { }

// Is this adding or updating?
public async Task AddOrUpdateClientConfig(ClientConfig config) { }

// Does this return raw JSON or parsed object?
public string GetResponse() { }
```

**Better Names:**
```csharp
public async Task<BaseResponse> LogUserActivityAsync(ActivityModel model) { }
public async Task<List<LogEntry>> GetActivityLogsForClientAsync(int clientId) { }
public async Task AddOrUpdateClientMarketConfigAsync(ClientConfig config) { }
public async Task<T> GetParsedResponseAsync<T>() { }
```

---

### Code Smell #10: Magic Numbers and Strings

**Location:** Multiple files

**Examples:**
```csharp
// What are these magic values?
if (cacheTimeout > 24) { }  // 24 = hours?
var prefetch = 1;            // Why 1?
TimeSpan.FromSeconds(180);   // Why 180? Hardcoded!

// Magic strings
if (status == "Active") { }
if (type == "OnActivityLogMessage") { }
_rabbitMqRepository.Publish(..., "r.oe.cachesync.ondeletecache");

// Should use constants or enums
const int CACHE_TIMEOUT_HOURS = 24;
enum MessageType { OnActivityLog, OnDeleteCache }
```

---

### Code Smell #11: Dead Code / Commented Code

**Location:** Various repositories

**Examples:**
```csharp
// Dead code marked for removal
// Note: Will Remove
public async Task<BaseResponse<bool>> AddActivityLog(...) { }

// Commented out code
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    app.UseHsts();
//}

// Unused variables
var response = await client.GetAsync(url);
var result = response.Content;  // Never used later

// Commented configuration
// "Backoffice": "Data Source=stg-server,1433..."
// "Backoffice": "Data Source=prd-server,1433..."
```

**Issue:**
- Confuses developers
- Takes up lines (harder to read)
- Why is it there? Fear of deletion? Old experiment?

---

## Inconsistent Patterns

### Code Smell #12: Inconsistent Async Patterns

**Location:** Various files

**Pattern 1 (Config.Service):**
```csharp
// Synchronous database calls
_repository.DeleteConfigTemplate(template);  // No async
await _context.SaveChangesAsync();
```

**Pattern 2 (API):**
```csharp
// Async database calls
await _repository.DeleteConfigTemplateAsync(template);
await _context.SaveChangesAsync();
```

**Pattern 3 (WebSocket.Service):**
```csharp
// Partial async
public async Task ProcessMessageAsync(Message msg)
{
    var data = _cache.GetData(key);  // Sync, should be GetDataAsync?
    await _hub.SendAsync("UpdateData", data);
}
```

**Issue:**
- Inconsistent approach across services
- Makes code harder to understand
- Could cause hidden issues (blocking async)

---

### Code Smell #13: Inconsistent Naming Conventions

**Location:** Multiple repositories

**Examples:**
```csharp
// PascalCase public method
public async Task GetClients() { }

// camelCase public method
public async Task getClientAsync(int id) { }

// Inconsistent suffixes
public async Task GetClientAsync() { }      // Async suffix
public async Task GetClientAsyncTask() { }  // Redundant suffix
public async Task GetClientAsyncAndWait() { }  // Wrong suffix

// Inconsistent async naming
ProcessActivityLog()      // No indication it's async
ProcessActivityLogAsync() // Clear it's async
ProcessActivityLogAwait() // Wrong convention
```

---

### Code Smell #14: Inconsistent Exception Handling

**Location:** Multiple repositories

**Pattern 1 (Silent failure):**
```csharp
try
{
    ProcessData();
}
catch
{
    // No logging, no rethrow
}
```

**Pattern 2 (Log and return error):**
```csharp
try
{
    ProcessData();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing");
    return new BaseResponse { StatusCode = StatusCode.Error };
}
```

**Pattern 3 (Log with context):**
```csharp
try
{
    ProcessData();
}
catch (DbUpdateException ex)
{
    _logger.LogError("DB error: {dbEx}", ex.InnerException);
    return new BaseResponse { StatusCode = StatusCode.Error };
}
```

**Issue:**
- Different error handling strategies
- Hard to debug (some errors silent)
- Inconsistent logging detail level

---

## Configuration Issues

### Code Smell #15: Configuration Not Validated

**Location:** `Backoffice.API/Startup.cs`

**Issue:**
```csharp
// Configuration loaded but never validated
var connectionString = configuration["ConnectionStrings:Backoffice"];
// If missing, silently null → Error at first use

services.AddDbContext<BackofficeContext>(options =>
    options.UseSqlServer(connectionString));
    
// Should validate at startup
```

**Better:**
```csharp
var options = configuration.GetSection("ConnectionStrings")
    .Get<ConnectionStringsOptions>();
    
if (string.IsNullOrEmpty(options?.Backoffice))
    throw new InvalidOperationException("Missing ConnectionStrings:Backoffice");
```

---

### Code Smell #16: Configuration Scattered Across Files

**Location:** Multiple files and config files

**Issue:**
```
appsettings.json
├─ ConnectionStrings
├─ API URLs
├─ API Keys
├─ Logging
├─ Security
└─ Features

appsettings.Development.json
├─ Overrides for dev

Program.cs
├─ More configuration settings

Startup.cs
├─ Additional setup

Controller
├─ Hardcoded values
```

**Issue:**
- Configuration scattered
- Hard to understand full picture
- Easy to miss settings
- Should use IOptions<T> with strongly-typed config classes

---

## Type Safety Issues

### Code Smell #17: Overuse of string-based Dictionary

**Location:** `Backoffice.WebSocket.API/Hubs/BackofficeHub.cs`

**Code:**
```csharp
private static readonly Dictionary<string, string> _users = new();
// Brittle: string keys can be anything
// No compile-time safety

_users["userId123"] = "connectionId456";
// Easy to misuse: _users["UserId123"] (different key)
```

**Better:**
```csharp
private static readonly Dictionary<int, string> _users = new();
// Type-safe: userId must be int

_users[123] = "connectionId456";  // Compile-time checked
```

---

### Code Smell #18: Object Used for Multiple Types

**Location:** Multiple repositories

**Code:**
```csharp
public object GetData(string key)
{
    return _cache.Get(key);  // Could be anything
}

// Usage
var result = GetData("config.123");
var config = (ConfigTemplate)result;  // Runtime cast!
```

**Better:**
```csharp
public T GetData<T>(string key) where T : class
{
    return _cache.Get<T>(key);  // Type-safe
}

// Usage
var config = GetData<ConfigTemplate>("config.123");  // Compile-time checked
```

---

## Missing Tests

### Code Smell #19: No Unit Tests

**Location:** Entire solution

**Observation:**
- No .Tests project in solution
- No test coverage
- Large complex methods without tests
- Repositories can't be unit tested (due to concrete dependencies)

**Risk:**
- Refactoring causes regressions
- Bug fixes create new bugs
- No regression prevention

---

### Code Smell #20: Difficult to Test Code

**Location:** All repositories

**Issues:**
```csharp
// Hard to test: creates dependencies internally
public RabbitMQRepository(IConfiguration config)
{
    _httpClient = new HttpClient();  // Can't mock
    _mongoClient = new MongoClient();  // Can't mock
}

// Method hard to test: static dependencies
public static IMongoDatabase _db;
// Can't isolate in unit test

// Hard to test: fire-and-forget
await PublishMessage();
// Can't verify message was published
```

---

## Summary of Code Smells

| # | Smell | Type | Severity | Locations |
|---|-------|------|----------|-----------|
| 1 | SRP violation | Design | HIGH | BackofficeRepository (2000 lines) |
| 2 | OCP violation | Design | MEDIUM | Worker.cs message handling |
| 3 | DIP violation | Design | MEDIUM | RabbitMQRepository construction |
| 4 | Repeated errors | Duplication | MEDIUM | All repositories |
| 5 | Repeated HttpClient | Duplication | MEDIUM | Multiple repos |
| 6 | Repeated config | Duplication | LOW | Multiple repos |
| 7 | Repeated cache keys | Duplication | LOW | Multiple places |
| 8 | Monolithic classes | Size | HIGH | Backoffice, RabbitMQ repos |
| 9 | Unclear names | Readability | MEDIUM | Various methods |
| 10 | Magic numbers | Clarity | MEDIUM | Hardcoded values |
| 11 | Dead code | Maintenance | MEDIUM | Scattered |
| 12 | Inconsistent async | Consistency | MEDIUM | Across services |
| 13 | Inconsistent naming | Consistency | LOW | Method names |
| 14 | Inconsistent errors | Consistency | MEDIUM | Exception handling |
| 15 | No config validation | Robustness | HIGH | Startup |
| 16 | Scattered config | Organization | MEDIUM | Multiple files |
| 17 | String-based dict | Type Safety | MEDIUM | WebSocket Hub |
| 18 | Object for multiple types | Type Safety | MEDIUM | Cache/repositories |
| 19 | No unit tests | Testing | HIGH | Entire solution |
| 20 | Hard to test | Testability | HIGH | All repositories |


## Dead code removed (DEVOPS-371)

SonarQube rules S1481 (unused local variable) and S4487 (unread private field) were cleaned up
across all services as part of DEVOPS-371. Only code with no observable effect was removed:

- **Unused locals** that held pure values (header/`HttpContext.Items` reads, in-memory LINQ
  projections, `new ModalOptions()`, discarded `JsonSerializer.Serialize` results, unused `out`
  variables now written as `out _`, and unused `catch (Exception ex)` variables now written as
  `catch (Exception)`).
- **Unused locals whose initializer has a side effect** keep the call and only drop the variable:
  awaited activity-log writes (`await BackofficeService.AddActivityLog(...)`), EF `Add`/`Update`
  calls, `new MailAddress(...)` (still used as e-mail validation), `CachePreload`, the logout HTTP
  call, `WebClient.UploadData`, the import progress poller (`_ = PollProgress(...)`), and the two
  never-awaited `CountAsync` calls in `BackOfficeRepository.GetAssignedCount`.
- **Unread private fields** in Backoffice.Web components and pages (breadcrumbs, page header
  models, unused modal title/icon plumbing in `MinimalistModal`, preloader flags, cached
  parameter copies), together with the assignments that only fed them. Three overrides that only
  set such a field were removed (`ConfigStateRow.OnInitializedAsync`,
  `ConfigMyTemplates.OnInitialized`, `MaintenanceV2.OnParametersSet`).
- The unused `BuildServiceProvider()` call (and its `ASP0000` pragma) at the top of
  `Backoffice.Web/Program.cs`.

### Remaining dead code (intentionally not touched)

- **Constructor-injected fields that are never read.** Removing them would change constructor
  signatures and DI wiring: `TradingController._activitySource`,
  `BackOfficeRepository._sportsbookRepository`, `MaintenanceRepository._isStaging` (its
  `bool.Parse(configuration["IsStaging"])` also acts as a startup check),
  `SportsbookRepository._activityLogRepository`, `TradingRepository._memCache`,
  `Login._jsRuntime`, `ErrorModel._logger`, `BackOfficeService._configuration`,
  `_authenticationService`, `_httpContextAccessor`, `_jsRuntime`,
  `TradingService._configuration`, `_httpContextAccessor`, and
  `TranslationService._httpContextAccessor`.
- **Backoffice.Shared** (`MessageBroker._externalQueue`, `_externalConsumerName`,
  `_externalRoutingKey`, and an unused `catch (Exception ex)`), since Shared changes are
  cross-service.
- **Export payloads kept for the pending "Migrate to api call" task** (the message object is
  built but the publish is commented out): MarketSettlement and MessageSettlement `Filter`,
  `ExportTemplateModal`, `ImportExportMarginModal`, `CancelBetHistory`, `ManualBetSettlement`.
- `MappingFixtures._alertModel`: the success alert is built but never rendered, which looks like
  an unfinished feature rather than dead code.
- `Backoffice.Utility` `BackofficeRepository` `clientCodes` (parses the JWT, which can throw on a
  malformed token).
- The never-awaited `CountAsync` calls in `GetAssignedCount` start queries on the shared
  `DbContext` concurrently with the awaited ones; they were kept as-is to avoid a behavior change.
