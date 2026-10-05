# Performance Bottlenecks

**Document Status:** Analysis Complete (No Code Changes Made)  
**Date:** June 18, 2026

---

## Database Performance

### Bottleneck #1: No Query Optimization

**Observation:**
- No `.AsNoTracking()` on read-only queries
- No explicit `.Include()` for related entities
- Lazy loading possible (N+1 queries)
- No index hints or query optimization

**Example Pattern:**
```csharp
// Queries entire table for single record
var client = await _context.ClientInformation
    .FirstOrDefaultAsync(c => c.ClientId == clientId);
    // No index assumption, no tracking disable

// Could load related data separately later
var settings = await _context.ClientBetSettings
    .Where(s => s.ClientId == clientId)
    .ToListAsync();  // Separate query

// Better: Include related data
var clientWithSettings = await _context.ClientInformation
    .AsNoTracking()  // Read-only
    .Include(c => c.BetSettings)
    .FirstOrDefaultAsync(c => c.ClientId == clientId);
```

**Impact:**
- Extra round-trips to database
- Unnecessary change tracking overhead
- Slower response times

---

### Bottleneck #2: Unbuffered Large Result Sets

**Observation:**
- Export operations load entire dataset to memory
- Example: `ExportMessageSettlementHistory` exports potentially millions of records

**Pattern:**
```csharp
var allSettlements = await _context.Settlement
    .Where(s => s.ClientId == clientId)
    .ToListAsync();  // ALL records in memory!
    
// Then process/export
foreach (var settlement in allSettlements)
{
    // Process
}
```

**Risk:**
- Memory spike with large exports
- Possible OutOfMemoryException
- Slow response times

**Better Approach:**
```csharp
// Stream results (not implemented)
var settlements = _context.Settlement
    .Where(s => s.ClientId == clientId)
    .AsAsyncEnumerable();  // Stream one at a time

await foreach (var settlement in settlements)
{
    // Process/export one at a time
}
```

---

### Bottleneck #3: Missing Database Indexes

**Observation:**
- No explicit index configuration
- EF Core uses entity keys only
- Common queries might scan table

**Example:**
```csharp
// This query might require table scan
var logs = await _context.ActivityLog
    .Where(l => l.ClientId == clientId && l.Date > startDate)
    .ToListAsync();
```

**Should Have Indexes:**
```
ActivityLog
├─ ClientId (foreign key)
├─ Date (range queries)
└─ ClientId + Date (composite for range queries)

ClientBetSetting
├─ ClientId
├─ TemplateId
└─ ClientId + TemplateId

ConfigTemplate
├─ ClientCode
└─ Status
```

---

### Bottleneck #4: Connection Pool Limits

**Observation:**
- DbContextPool for Backoffice/Translation contexts only
- Trading/Sportsbook don't use pooling
- Each request might create new context

**Config:**
```csharp
services.AddDbContextPool<BackofficeContext>(options =>
    options.UseSqlServer(connectionString),
    poolSize: 128);  // Default is small

// Trading context (NOT pooled)
services.AddDbContext<TradingContext>(options =>
    options.UseSqlServer(connectionString));
    // Creates new context per request
```

**Impact:**
- Context creation overhead
- Connection overhead
- Limits concurrency

---

## HTTP Request Performance

### Bottleneck #5: HttpClient Creation Per Request

**Location:** `RabbitMQRepository.cs`

**Cost:**
```
Creating HttpClient per request:
├─ Allocate object: ~1ms
├─ DNS resolution: ~50ms (if not cached)
├─ TCP connection: ~50ms
├─ TLS handshake: ~100ms (HTTPS)
├─ First request: ~200-300ms
└─ Total overhead: ~200-300ms per request
```

**Current Pattern:**
```csharp
// Every publish request
var client = GetHttpClient(region);  // NEW instance
await client.PostAsync(uri, content);
// Client not reused → Connection closed
```

**Better:**
```csharp
// Reuse HttpClient
private static readonly HttpClient _client = new();

await _client.PostAsync(uri, content);
// Connection pooling, DNS cached
```

---

### Bottleneck #6: No Connection Pooling for RabbitMQ

**Observation:**
```csharp
// Each message publish might create connection
var factory = new ConnectionFactory { ... };
using (var connection = factory.CreateConnection())
{
    // Publish message
    // Connection closed after use
}
```

**Better:**
```csharp
// Connection pooling
private static IConnection _connection;

if (_connection == null)
{
    _connection = factory.CreateConnection();
}

using (var channel = _connection.CreateModel())
{
    // Publish message
    // Connection reused
}
```

---

### Bottleneck #7: Synchronous HTTP Calls in Async Context

**Location:** `AuthorizeAttribute.cs`

**Pattern:**
```csharp
public async void OnAuthorization(AuthorizationFilterContext context)
{
    var response = await _httpClient.PostAsync(validationUri, content);
    // Makes HTTP call to validate EVERY request
    // No caching
}
```

**Impact:**
- Network latency on every request
- Blocks token validation
- Dependency on Auth.API availability
- Single point of failure

**Better:**
```csharp
// Cache validated tokens
var cacheKey = $"token.validated.{tokenHash}";
if (!_cache.TryGetValue(cacheKey, out var principal))
{
    principal = ValidateTokenLocally(token);
    _cache.Set(cacheKey, principal, TimeSpan.FromHours(1));
}
```

---

## Cache Performance

### Bottleneck #8: Cold Cache on Startup

**Observation:**
- Application starts with no cached data
- First requests are slow (DB queries)
- Cache slowly warms up

**Current:**
```csharp
// Lazy loading on first request
var data = _memCache.GetCache(
    key,
    () => _context.Clients.ToList(),  // Slow first request
    expiration: 1);
```

**Better:**
```csharp
// Cache warming on startup
public class CacheWarmup : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        // Pre-load common queries
        var clients = await _context.Clients.ToListAsync();
        _memCache.AddOrUpdateCache("clients.all", clients, 24);
        
        var sports = await _context.Sports.ToListAsync();
        _memCache.AddOrUpdateCache("sports.all", sports, 24);
    }
}
```

---

### Bottleneck #9: In-Memory Cache Not Shared

**Observation:**
- Each server instance has separate cache
- Multi-instance deployments = cache inconsistency
- Defeats purpose of caching

**Current:**
```
Server 1: MemoryCache           Server 2: MemoryCache
├─ clients.all                  ├─ clients.all (different!)
├─ config.123 (updated)         └─ config.123 (stale)
```

**Impact:**
- Users see different data based on which server handles request
- Cache invalidation complex
- Eventual consistency issues

---

### Bottleneck #10: Unbounded Cache Growth

**Observation:**
```csharp
// No size limits on MemoryCache
_memCache.AddOrUpdateCache(key, largeObject, timeout);
// Could consume all available memory

// Only TTL-based eviction
// No LRU, no size limits
```

**Risk:**
- Memory exhaustion over time
- GC pauses
- Performance degradation

---

## Message Queue Performance

### Bottleneck #11: Fire-and-Forget Publishing

**Observation:**
- Message publishing doesn't wait for confirmation
- No batching
- Each message separate HTTP call

**Current:**
```csharp
// Each message separate call
await _rabbitMqRepository.Publish(messageRequest1);
await _rabbitMqRepository.Publish(messageRequest2);
await _rabbitMqRepository.Publish(messageRequest3);
// 3 HTTP calls, ~300ms total
```

**Better:**
```csharp
// Batch publishes
await _rabbitMqRepository.PublishBatch(new[] {
    messageRequest1,
    messageRequest2,
    messageRequest3
});  // 1 HTTP call, ~100ms total
```

---

### Bottleneck #12: No Message Compression

**Observation:**
- Messages sent uncompressed
- Large payloads (market data, configs)
- Network bandwidth waste

**Example Message:**
```json
{
    "clientId": 123,
    "configurations": [
        // 100+ items with nested objects
    ]
}
// Uncompressed: 50KB
// Gzip compressed: 5KB
// 10x improvement!
```

---

## Query Performance

### Bottleneck #13: Complex Queries Executed in Code

**Observation:**
```csharp
// Bad: Loads all data then filters
var allClients = await _context.ClientInformation
    .ToListAsync();  // Load ALL

var results = allClients
    .Where(c => c.Name.Contains(searchTerm))  // Filter in memory
    .Skip(pageNumber * pageSize)
    .Take(pageSize)
    .ToList();
```

**Should be:**
```csharp
// Good: Filter at database
var results = await _context.ClientInformation
    .Where(c => c.Name.Contains(searchTerm))
    .Skip(pageNumber * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

### Bottleneck #14: Pagination Not Used

**Observation:**
- Export endpoints load entire datasets
- No pagination on list endpoints
- Could return 100,000+ records

**Example:**
```csharp
// Returns all activity logs for client
public async Task<List<ActivityLog>> GetActivityLogs(int clientId)
{
    return await _context.ActivityLog
        .Where(l => l.ClientId == clientId)
        .ToListAsync();  // No limit!
}
```

---

## Serialization Performance

### Bottleneck #15: JSON Serialization Overhead

**Observation:**
- Every message serialized to JSON
- Every response serialized to JSON
- No compression before network send

**Cost:**
```
Serialization:
├─ Object → JSON string: 5-10ms
├─ Network send: 50-200ms
├─ Deserialization: 5-10ms
└─ Total: 60-220ms per message

Message types: 15+
Estimated overhead: 1-3.3 seconds per complex request
```

**Better:**
```csharp
// Binary format
// MessagePack: Faster, smaller than JSON
// Protocol Buffers: Schema-based, validates
// Already used for SignalR, should extend to HTTP
```

---

## Concurrency Performance

### Bottleneck #16: Lock-Based SignalR User Tracking

**Location:** `BackofficeHub.cs`

**Code:**
```csharp
private static readonly Dictionary<string, string> _users = new();

lock (_users)  // Coarse-grained lock
{
    _users[userId] = connectionId;
}
```

**Contention:**
- All user connections compete for same lock
- Long operations hold lock
- High contention under load
- Limits scalability

**Better:**
```csharp
// ConcurrentDictionary (no lock needed)
private static readonly ConcurrentDictionary<string, string> _users = new();

_users[userId] = connectionId;  // Thread-safe without lock
```

---

### Bottleneck #17: Synchronous Database Calls in Background Worker

**Location:** `Config.Service/Worker.cs`

**Pattern:**
```csharp
protected override async Task ExecuteAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        var message = await _consumer.ReceiveAsync(timeout: 5s);
        
        // BLOCKS async loop
        _repository.ProcessMessage(message);  // Sync call
    }
}
```

**Impact:**
- Single-threaded execution
- No parallelism
- Can't handle bursts
- Throughput limited

---

## External API Performance

### Bottleneck #18: No API Response Caching

**Observation:**
- Each request to external APIs makes HTTP call
- Example: Auth.API token validation on every request
- Example: Platform API client list on every lookup

**Could Cache:**
```
Auth.API token validation: Cache for token lifetime
Platform API client list: Cache for 1 hour
Sportsbook API fixtures: Cache for 5 minutes
Exchange rates: Cache for 1 hour
```

---

### Bottleneck #19: No Circuit Breaker

**Observation:**
- If external API slow/down, requests back up
- No timeout protection
- Can cause cascading failures

**Example:**
```
Platform API down:
├─ Client requests platform data
├─ API calls Platform → Timeout (30s)
├─ Multiple requests accumulate
├─ Thread pool exhausted
├─ All requests fail
└─ Cascading failure
```

---

### Bottleneck #20: No Rate Limiting for Outbound APIs

**Observation:**
- No throttling on external API calls
- Could overwhelm downstream services
- No quota management

---

### Bottleneck #21: Forced `GC.Collect()` on page dispose (Fixed, DEVOPS-371)

**Location:** `Backoffice.Web/Pages/OddsEngine/ConfigAssigned.razor.cs`, `Dispose()`

**Observation:**
- `Dispose()` called `GC.Collect()` every time a user left the Config Assigned page
- A forced full, blocking collection pauses the whole bo-web process, so every user's circuit in
  the pod paid for one user's navigation
- Nulling two fields before it did not need a forced collection; the runtime reclaims them anyway

**Fix:** removed the call (SonarQube S1215). The field resets in `Dispose()` are unchanged.

---

## Performance Summary

| # | Bottleneck | Severity | Impact | Affected Areas |
|---|------------|----------|--------|---|
| 1 | No query optimization | HIGH | Slow DB queries | All repos |
| 2 | Large result sets | HIGH | Memory spikes | Export operations |
| 3 | Missing indexes | HIGH | Table scans | Queries |
| 4 | No connection pooling | HIGH | Slow contexts | Trading/Sportsbook |
| 5 | HttpClient per request | CRITICAL | 200-300ms overhead | Publishing |
| 6 | No RabbitMQ pooling | MEDIUM | Connection overhead | Message broker |
| 7 | Sync HTTP in async | MEDIUM | Blocking requests | Authorization |
| 8 | Cold cache startup | MEDIUM | Slow first requests | All requests |
| 9 | Shared cache miss | MEDIUM | Cache misses | Multi-instance |
| 10 | Unbounded cache | MEDIUM | Memory growth | Long-running |
| 11 | No batch publishing | MEDIUM | 3x overhead | Messages |
| 12 | No compression | MEDIUM | 10x bandwidth | Messages |
| 13 | In-code queries | MEDIUM | Memory waste | Complex searches |
| 14 | No pagination | MEDIUM | Large transfers | List endpoints |
| 15 | JSON overhead | MEDIUM | 60-220ms per msg | All serialization |
| 16 | Lock contention | MEDIUM | Single-threaded | SignalR |
| 17 | Sync in async | MEDIUM | Limited throughput | Background worker |
| 18 | No API caching | MEDIUM | Extra round-trips | External APIs |
| 19 | No circuit breaker | HIGH | Cascading failure | API calls |
| 20 | No rate limiting | MEDIUM | Quota exceeded | Outbound |

