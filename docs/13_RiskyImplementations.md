# Risky Implementations

**Document Status:** Analysis Complete (No Code Changes Made)  
**Date:** June 18, 2026

---

## Threading & Concurrency Risks

### Risk #1: Static MongoDB Connection (Thread-Safety)

**Location:** `Backoffice.API/Repositories/SportsbookRepository.cs`

**Code:**
```csharp
private static IMongoDatabase _db;  // STATIC = shared across all instances

public SportsbookRepository()
{
    if (_db == null)  // RACE CONDITION
    {
        var client = new MongoClient(connectionString);
        _db = client.GetDatabase("Sportsbook");
        _sportLookupCollection = _db.GetCollection<SportLookup>("SportLookup");
    }
}
```

**Race Condition Timeline:**
```
Thread 1: Check _db == null? YES
Thread 2: Check _db == null? YES
Thread 1: Create MongoClient #1
Thread 2: Create MongoClient #2
Thread 1: Set _db = client#1.GetDatabase()
Thread 2: Set _db = client#2.GetDatabase()  // Overwrites!
Result: Unpredictable behavior, possible connection leak
```

**Risk Level:** ⚠️ CRITICAL

**Consequences:**
- Multiple MongoClients created (wasted resources)
- Connection management issues
- Potential thread-safety violations
- Hard to reproduce (race condition)

**Better Pattern:**
```csharp
private static readonly Lazy<IMongoDatabase> _db = 
    new Lazy<IMongoDatabase>(() => 
    {
        var client = new MongoClient(connectionString);
        return client.GetDatabase("Sportsbook");
    });
```

---

### Risk #2: Simple Lock() for User Tracking

**Location:** `Backoffice.WebSocket.API/Hubs/BackofficeHub.cs`

**Code:**
```csharp
private static readonly Dictionary<string, string> _users = new();

public override Task OnConnectedAsync()
{
    var userId = Context.GetHttpContext()?.Request.Query["userId"];
    
    lock (_users)  // Simple lock
    {
        _users[userId] = Context.ConnectionId;
    }
    
    return base.OnConnectedAsync();
}
```

**Risks:**
1. **Deadlock Risk** - If code in lock calls code that locks same object
2. **Performance** - Lock contention under load
3. **Not Distributed** - Doesn't work with multiple servers
4. **Hidden Bugs** - Difficult to debug lock issues

**Example Deadlock Scenario:**
```
Thread 1: lock(_users) → Calls SaveUserAsync() → Needs database → Waits
Thread 2: Holds database lock → Tries lock(_users) → Waits
Result: Deadlock
```

**Risk Level:** ⚠️ HIGH

---

### Risk #3: No Synchronization of Shared State

**Observation:**
- RabbitMQ consumer loops run in background tasks
- Multiple workers might process same message
- No coordination between workers

**Example:**
```csharp
// Config.Service Worker
while (!stoppingToken.IsCancellationRequested)
{
    var message = await _consumer.ReceiveAsync();
    // Multiple service instances could process same message
    _repository.DeleteConfigTemplate(message);  // Race!
}
```

**Risks:**
- Duplicate processing
- Inconsistent state
- Double-updates to database

---

## Resource Management Risks

### Risk #4: HttpClient Not Disposed

**Location:** `Backoffice.API/Repositories/RabbitMQRepository.cs`

**Code:**
```csharp
private HttpClient GetHttpClient(string region)
{
    var client = new HttpClient()  // NEW INSTANCE
    {
        BaseAddress = new Uri(uri)
    };
    return client;  // NEVER DISPOSED
}

// Usage
var client = GetHttpClient("HK");
await client.PostAsync(url, content);  // HttpClient leaked!
```

**Resource Leak:**
```
Per request: HttpClient created
├─ HttpMessageHandler allocated
├─ Socket allocated
└─ Not released

With 1000 requests/second:
├─ 1000 HttpClients created/second
├─ Sockets exhausted in minutes
└─ Service becomes unresponsive
```

**Risk Level:** ⚠️ CRITICAL

**Consequences:**
- Socket exhaustion: "Only one usage of each socket address permitted"
- Memory leak: 1MB+ per HttpClient
- Service becomes unresponsive

---

### Risk #5: DbContext Not Disposed

**Location:** `Backoffice.Config.Service/Repository/BackofficeRepository.cs`

**Code:**
```csharp
public BackofficeRepository(IConfiguration configuration, 
    ILogger<BackofficeRepository> logger)
{
    var optionsBuilder = new DbContextOptionsBuilder<BackofficeContext>()
        .UseSqlServer(_configuration["ConnectionStrings:Backoffice"]);
    
    _backofficeContext = new BackofficeContext(optionsBuilder.Options);
    // Never disposed in long-running background service!
}

// Every message processed reuses same DbContext
public async Task DeleteConfigTemplate(ConfigTemplate template)
{
    await _backofficeContext.ConfigTemplate.AddAsync(template);
    await _backofficeContext.SaveChangesAsync();
    // DbContext not reset, change tracking state accumulates
}
```

**Risk Level:** ⚠️ CRITICAL

**Consequences:**
- Change tracker memory accumulation
- Identity map grows (all entities ever loaded)
- Memory leak in background service
- Unexpected behavior with second message

**Example:**
```
Message 1: Delete ConfigTemplate(1)
├─ DbContext tracks entity
├─ SaveChangesAsync() completes
└─ Entity still in change tracker

Message 2: Delete ConfigTemplate(2)
├─ DbContext now has 2 entities in tracker
├─ Both entities might be affected
└─ Unpredictable behavior

Message 100: Delete ConfigTemplate(100)
├─ DbContext has 100 entities in tracker
├─ Memory bloated
├─ Performance degraded
└─ OutOfMemoryException possible
```

---

### Risk #6: MongoDB Connection Not Pooled

**Observation:**
```csharp
// Each SportsbookRepository instance creates connection
var client = new MongoClient(connectionString);
_db = client.GetDatabase("Sportsbook");

// With multiple instances/requests:
Request 1: Create MongoClient #1
Request 2: Create MongoClient #2
Request 3: Create MongoClient #3
// Connection exhaustion
```

**Risk Level:** ⚠️ HIGH

---

## Data Consistency Risks

### Risk #7: Fire-and-Forget Message Publishing

**Location:** All repositories

**Code:**
```csharp
// Database updated
await _backofficeContext.ClientInformation.AddAsync(client);
await _backofficeContext.SaveChangesAsync();

// Message published (no error handling)
await _rabbitMqRepository.Publish(new MessageRequest
{
    MessageType = "OnClientUpdated",
    Payload = JsonSerializer.Serialize(client)
}, region);

// If publish fails:
// ✓ Database has new data
// ✗ Message never sent
// = Stale caches, inconsistent state
```

**Risk Level:** ⚠️ CRITICAL

**Consequences:**
- Split-brain: Some systems see new data, others see old
- Cache invalidation failures
- Users see stale data
- No retry mechanism

**Example Scenario:**
```
1. API updates client configuration
2. Database: Configuration saved ✓
3. Message publish: Network timeout ✗
4. Cache invalidation: Never sent
5. WebSocket update: Never broadcast
6. Client A: Sees new configuration ✓
7. Client B: Sees stale configuration ✗
   (Both connected to different services/caches)
```

---

### Risk #8: No Message Idempotency

**Location:** Background workers

**Code:**
```csharp
// Message consumed
switch (messageType)
{
    case "UpdateClientMargin":
        var margin = JsonSerializer.Deserialize<long>(payload);
        _backofficeRepository.UpdateClientMargin(margin);  // Assumes first time
        break;
}
```

**Risk:**
- If message reprocessed, margin updated again
- No idempotency key tracking
- Duplicate processing causes data corruption

**Example:**
```
Message: UpdateClientMargin(100)

First consumption: ClientMargin = 100 ✓
Redelivery (message reprocessed): ClientMargin = 100 again (if not tracked)

Expected: Margin updated to 100 once
Actual: Could be updated multiple times (depending on logic)
```

---

### Risk #9: NoAck Message Processing

**Observation:**
```csharp
// Message might not be properly acknowledged
// If processing crashes, message could be lost
```

**Risk Level:** ⚠️ MEDIUM

---

## Security Risks

### Risk #10: Bearer Token Extracted But Not Fully Validated

**Location:** `Backoffice.API/ActionFilter/ApiKeyFilter.cs` line 27-29

**Code:**
```csharp
var token = context.HttpContext.Request.Headers[HeaderNames.Authorization]
    .ToString().Replace("Bearer", "").Trim();

// Token extracted but NEVER VALIDATED in ApiKeyFilter
// Only validated later in AuthorizeAttribute (inconsistently)
```

**Risk:**
- Token passed through without verification
- Invalid tokens might reach endpoints
- Security bypass possible

**Risk Level:** ⚠️ CRITICAL

---

### Risk #11: API Key in Configuration File

**Location:** `appsettings.json`

**Code:**
```json
{
    "Security": {
        "ApiKey": "9a8ad73dd129bf49754242e44988b3e7e39f0a05"
    }
}
```

**Risks:**
- Plaintext in source code
- Visible to anyone with repo access
- Cannot be rotated without code change
- Already in version control history

**Risk Level:** ⚠️ CRITICAL

---

### Risk #12: Developer Exception Page Always Enabled

**Location:** `Backoffice.Web/Program.cs` line 54

**Code:**
```csharp
app.UseDeveloperExceptionPage();  // ALWAYS ON (comment says "temp")
```

**Exposes:**
- Stack traces
- SQL queries
- Internal paths
- Configuration details
- Secrets potentially

**Risk Level:** ⚠️ CRITICAL

---

### Risk #13: No Input Validation

**Location:** Most controllers

**Code:**
```csharp
[HttpPost]
public async Task<IActionResult> CreateClient(ClientModel model)
{
    // No validation of model properties
    // Could save null name, invalid email, negative ID, etc.
    
    _context.ClientInformation.Add(model);
    await _context.SaveChangesAsync();
}
```

**Risks:**
- Invalid data in database
- Downstream processing fails
- No error messages to user

**Risk Level:** ⚠️ MEDIUM

---

## Deployment & Operations Risks

### Risk #14: No Configuration Validation at Startup

**Location:** `Startup.cs`

**Risks:**
- Service starts with broken configuration
- Fails at first request
- Hard to diagnose

**Example:**
```
Missing: ConnectionStrings:Backoffice
Result: Service starts fine
First request: NullReferenceException
Diagnosis: Difficult (not clear config is missing)
```

**Risk Level:** ⚠️ HIGH

---

### Risk #15: Hardcoded Timeouts and Limits

**Observation:**
```csharp
// Hardcoded values
const int TIMEOUT_SECONDS = 180;
const int PAGE_SIZE = 100;
const int CACHE_TTL_HOURS = 1;
```

**Risks:**
- Can't adjust without recompiling
- Different values across services
- Bulk operations might timeout
- No flexibility for production tuning

**Risk Level:** ⚠️ MEDIUM

---

### Risk #16: No Health Checks

**Observation:**
```csharp
// Basic health check endpoint
services.AddHealthChecks();

// But no checks for:
// - Database connectivity
// - RabbitMQ availability
// - External API reachability
```

**Risks:**
- Service marked as healthy when dependencies down
- Load balancer routes requests to broken service
- Cascading failures

**Risk Level:** ⚠️ HIGH

---

### Risk #17: Manual DbContext Creation in Background Service

**Location:** `Config.Service/Repository/BackofficeRepository.cs`

**Risks:**
- Bypasses DI container
- Not testable
- Can't use DbContextPool benefits
- Manual connection management error-prone

**Risk Level:** ⚠️ HIGH

---

## Observability Risks

### Risk #18: Inconsistent Logging

**Observation:**
- Some errors logged, others silent
- Different logging levels across repos
- No correlation IDs
- Some functions don't log at all

**Risks:**
- Hard to diagnose production issues
- Incomplete audit trail
- Missing error context

**Risk Level:** ⚠️ MEDIUM

---

### Risk #19: Incomplete Activity Logging

**Location:** `BackofficeRepository.cs` line 65

**Code:**
```csharp
// Note: Will Remove
public async Task<BaseResponse<bool>> AddActivityLog(ActivityLogModel request)
```

**Risks:**
- Function marked for removal but still called
- Deprecation unclear
- If removed, breaks callers
- Unclear what log data is actually being recorded

**Risk Level:** ⚠️ MEDIUM

---

## Architecture Risks

### Risk #20: Single Point of Failure (SignalR Hub)

**Location:** `Backoffice.WebSocket.API`

**Code:**
```csharp
private static readonly Dictionary<string, string> _users = new();
```

**Risks:**
- Single server only (not distributed)
- If hub goes down, all real-time connections lost
- Users see stale data
- No horizontal scaling for WebSocket layer

**Risk Level:** ⚠️ HIGH

---

### Risk #21: Monolithic Repositories

**Location:** All repositories (especially BackofficeRepository)

**Risks:**
- 2000+ line classes hard to change safely
- High coupling
- Difficult to test
- Risk of regression on any change

**Risk Level:** ⚠️ MEDIUM

---

### Risk #22: Activity Log Scaling Risk

**Location:** Activity logging everywhere

**Risks:**
- Activity logs grow unbounded
- Database table could become too large
- Queries slow over time
- No archiving strategy

**Risk Level:** ⚠️ MEDIUM

---

### Risk #23: Cache Invalidation Complexity

**Observation:**
- Multi-instance deployments
- Async RabbitMQ invalidation
- Eventual consistency issues
- Race conditions possible

**Example Race:**
```
T1: Instance A writes config → Cache marked invalid
T2: Instance B writes config → Cache marked invalid
T3: Message arrives at Instance A → Invalidates
T4: Message arrives at Instance B → Invalidates
T5: New request to Instance A → Cache miss → Loads (might be outdated!)

Timing-dependent behavior → Hard to reproduce bugs
```

**Risk Level:** ⚠️ MEDIUM

---

## Summary of Risky Implementations

| # | Risk | Type | Severity | Impact |
|---|------|------|----------|--------|
| 1 | MongoDB static connection | Threading | CRITICAL | Unpredictable |
| 2 | Simple lock() on dict | Threading | HIGH | Deadlock/perf |
| 3 | No shared state sync | Threading | MEDIUM | Race conditions |
| 4 | HttpClient leak | Resources | CRITICAL | Socket exhaustion |
| 5 | DbContext leak | Resources | CRITICAL | Memory leak |
| 6 | MongoDB no pool | Resources | HIGH | Connection leak |
| 7 | Fire & forget publish | Consistency | CRITICAL | Stale data |
| 8 | No idempotency | Consistency | MEDIUM | Double-processing |
| 9 | NoAck messages | Reliability | MEDIUM | Lost messages |
| 10 | Token not validated | Security | CRITICAL | Auth bypass |
| 11 | API key in config | Security | CRITICAL | Compromise |
| 12 | DevEx enabled | Security | CRITICAL | Info leak |
| 13 | No input validation | Quality | MEDIUM | Bad data |
| 14 | No config validation | Ops | HIGH | Late failure |
| 15 | Hardcoded limits | Flexibility | MEDIUM | Can't tune |
| 16 | No health checks | Ops | HIGH | Silent failures |
| 17 | Manual DbContext | Architecture | HIGH | Not testable |
| 18 | Inconsistent logging | Ops | MEDIUM | Hard to debug |
| 19 | Unclear logging | Ops | MEDIUM | Missing data |
| 20 | SignalR hub SPOF | Architecture | HIGH | Connection loss |
| 21 | Monolithic repos | Design | MEDIUM | Hard to change |
| 22 | Activity log scale | Data | MEDIUM | Slow queries |
| 23 | Cache invalidation | Consistency | MEDIUM | Race conditions |

---

## Recommendations

### Immediate (Week 1)
1. Fix async void in AuthorizeAttribute
2. Move API keys to secrets manager
3. Disable developer exception page in production
4. Use Lazy<T> for MongoDB connection

### Short-term (Sprint 1-2)
1. Fix HttpClient creation (use factory)
2. Fix DbContext in Config.Service
3. Add message idempotency
4. Implement startup config validation
5. Add health checks

### Medium-term (Sprint 3-4)
1. Refactor large repositories
2. Add input validation
3. Implement consistent logging
4. Add correlation IDs
5. Migrate to distributed caching

### Long-term (Future)
1. Implement saga pattern for distributed transactions
2. Add message dead letter queues
3. Distribute SignalR hub (scale WebSocket)
4. Archive old activity logs
5. Implement circuit breaker for external APIs

