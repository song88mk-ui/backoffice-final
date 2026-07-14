# Caching Strategy

**Document Status:** Analysis Complete (No Code Changes Made)  
**Date:** June 18, 2026

## Caching Architecture

### Cache Provider

**Provider:** `System.Runtime.Caching.MemoryCache`  
**Lifetime:** Application
**Scope:** In-process (single server only)
**Thread Safety:** Thread-safe (built into MemoryCache)

### Caching Layers

```
Request
  ↓
[Layer 1] Browser Cache
  ├─ HTTP Cache Headers
  └─ Browser local storage
  
  ↓
[Layer 2] Application Memory Cache (MemoryCache)
  ├─ Backoffice.API
  ├─ Backoffice.Web
  └─ Background services
  
  ↓
[Layer 3] Database Cache (MongoDB)
  └─ Sportsbook collection cache
  
  ↓
[Layer 4] Source of Truth
  └─ SQL Server databases
```

---

## IMemCache Interface

### Method Signatures

```csharp
public interface IMemCache
{
    // Add or update entry with TTL in hours
    void AddOrUpdateCache<T>(
        string key, 
        T cacheObject, 
        int cacheTimeout);
    
    // Add or update with absolute expiration date
    void AddOrUpdateCache<T>(
        string key, 
        T cacheObject, 
        DateTimeOffset date);
    
    // Lazy-load pattern: Get or populate
    T GetCache<T>(
        string key, 
        Func<T> repositoryCall,     // Populate if missing
        int expiration) 
        where T : class;
    
    // Simple get (no auto-populate)
    T GetCache<T>(string key) 
        where T : class;
    
    // Retrieve with fallback value
    KeyValuePair<bool, long> GetCache(string key);
    
    // Remove entry
    void DeleteCache(string key);
}
```

---

## Caching Patterns

### Pattern 1: Lazy Loading with Auto-Populate

```csharp
public async Task<BaseResponse<ClientList>> GetClients()
{
    var cacheKey = "clients.all";
    
    var cached = _memCache.GetCache<ClientList>(
        cacheKey,
        () => {
            // This runs if cache miss
            return _context.Clients
                .AsNoTracking()
                .ToList();
        },
        expiration: 1  // 1 hour
    );
    
    return new BaseResponse { Result = cached };
}
```

**Behavior:**
1. Check cache for key
2. If hit: return cached data
3. If miss: execute lambda function
4. Store result in cache with 1-hour TTL
5. Return result

### Pattern 2: Explicit Set

```csharp
var data = await _repository.GetData();

_memCache.AddOrUpdateCache(
    key: "config.template.123",
    cacheObject: data,
    cacheTimeout: 24  // 24 hours
);
```

### Pattern 3: Date-Based Expiration

```csharp
_memCache.AddOrUpdateCache(
    key: "temp.token",
    cacheObject: token,
    date: DateTimeOffset.UtcNow.AddHours(2)
);
```

### Pattern 4: Cache Invalidation

```csharp
// When data changes, invalidate
_memCache.DeleteCache("clients.all");
_memCache.DeleteCache("config.template.123");

// Broadcast invalidation via RabbitMQ
await _messageBroker.Publish(new OnDeleteCacheMessage
{
    CacheKey = "config.template.123",
    ClientId = 123
});
```

---

## Cached Data

### By Service

**Backoffice.API caches:**
```
clients.*
├─ clients.all
├─ clients.{clientId}
└─ clients.{region}

config.*
├─ config.template.{templateId}
├─ config.marketplace.{clientId}
└─ config.margin.{clientId}

translations.*
├─ translations.{languageCode}
└─ translations.all

sports.*
├─ sports.all
├─ sports.{sportId}
└─ sports.hierarchy

exchange.*
└─ exchange.rates.{currency}
```

**Backoffice.Web caches:**
```
ui.*
├─ ui.user.{userId}
├─ ui.permissions.{userId}
└─ ui.settings.{userId}

Local Storage (Blazored.LocalStorage):
├─ auth.token
├─ ui.preferences
└─ recent.searches
```

**Background Services cache:**
```
processing.*
├─ batch.{jobId}
├─ settlement.{settlementId}
└─ export.{exportId}
```

---

## Cache Expiration Strategy

### TTL (Time-To-Live) by Category

| Data Category | TTL | Rationale |
|---------------|-----|-----------|
| Client list | 1 hour | Changes infrequently |
| Client settings | 30 minutes | Frequently updated |
| Market data | 5 minutes | Updates often (real-time) |
| Sports hierarchy | 24 hours | Never changes |
| Exchange rates | 1 hour | Updated daily |
| Configuration | 2-4 hours | Centrally managed |
| Activity logs | 15 minutes | Audit purposes |
| Translations | 24 hours | Rarely change |
| Permissions | 1 hour | Security sensitive |
| Temporary data | 5 minutes | Working sets |

### Cache Invalidation Triggers

```csharp
// Event-driven invalidation
switch (action)
{
    case "ClientUpdated":
        _memCache.DeleteCache($"clients.{clientId}");
        _memCache.DeleteCache("clients.all");
        
        // Broadcast via RabbitMQ
        await PublishCacheInvalidation(clientId);
        break;
        
    case "ConfigTemplateUpdated":
        _memCache.DeleteCache($"config.template.{templateId}");
        break;
}
```

### Scheduled Invalidation (Not Implemented)

```csharp
// Would run periodically
// Currently: TTL-based only
ScheduleTask(() =>
{
    _memCache.DeleteCache("market.data");  // Refresh market data
}, interval: TimeSpan.FromMinutes(5));
```

---

## Cache Hit Rate

### Estimated by Category

| Category | Hit Rate | Notes |
|----------|----------|-------|
| Clients | 95% | Added, rarely queried |
| Config | 85% | Some variance per client |
| Sports | 99% | Static, long TTL |
| Exchange Rates | 80% | Changes daily |
| Market Data | 60% | Real-time, short TTL |
| Average | 84% | ~5 cache checks per request |

### Performance Impact

```
Cache Miss Cost:
├─ Database query: 50-200ms
├─ JSON serialization: 5-10ms
├─ Network: included in query
└─ Total: 50-250ms (depends on query)

Cache Hit Cost:
├─ Dictionary lookup: <1ms
├─ Deserialization (if needed): 1-5ms
└─ Total: <1-5ms

Improvement: 50-250x faster
```

---

## Memory Usage

### Estimation by Environment

**Development (Single instance):**
```
Typical Memory Usage:
├─ Client data: 10-50MB
├─ Config templates: 20-100MB
├─ Sports hierarchy: 50-100MB
├─ Market data: 200-500MB
├─ Other: 50-100MB
└─ Total: ~330-750MB
```

**Production (Scaling):**
```
Multiple instances:
├─ Instance 1: 330-750MB
├─ Instance 2: 330-750MB
├─ Instance 3: 330-750MB
└─ Total: 1GB-2.25GB per 3 instances
```

### Memory Pressure Scenarios

**High Load:**
- Client count: 100+ instances
- Memory per instance: 1-2GB
- Potential: Memory exhaustion

**Risk:** MemoryCache doesn't limit total size (by default)

**Mitigation:**
```csharp
// Not implemented
// Could use:
var cachePolicy = new CacheItemPolicy
{
    AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1),
    SlidingExpiration = TimeSpan.FromMinutes(30)  // Extend if accessed
};
```

---

## Cache Invalidation Strategy

### Current Strategy: Event-Driven + TTL

```
1. Change occurs in API
   ↓
2. Database updated
   ↓
3. Cache entry deleted (immediate)
   ↓
4. RabbitMQ message published (async)
   ↓
5. WebSocket.Service broadcasts (real-time)
   ↓
6. UI updates (if subscribed)
   ↓
7. Next request: Cache miss, reload
```

### RabbitMQ Cache Invalidation Message

```csharp
var cacheMessage = new OnDeleteCacheMessage
{
    ClientId = 123,
    ClientCode = "CLIENT123",
    CacheType = CacheType.ClientConfig,
    CacheKey = "cc:123"
};

// Publish to RabbitMQ
await _messageBroker.Publish(new MessageRequest
{
    Payload = JsonSerializer.Serialize(cacheMessage),
    MessageType = "OnDeleteCache",
    RoutingKey = "r.oe.cachesync.ondeletecache"
});
```

### Issues with Current Strategy

1. **Single Instance** - Cache not shared across server instances
2. **Eventual Consistency** - Async invalidation, not immediate
3. **Message Loss** - Fire-and-forget could lose invalidation messages
4. **Manual Invalidation** - Developer must remember to invalidate

---

## Distributed Caching (Not Implemented)

### Current Limitation

```
Server Instance 1               Server Instance 2
├─ MemoryCache                  ├─ MemoryCache
├─ Client list cache            ├─ Client list cache (different!)
└─ Config cache                 └─ Config cache (different!)

Problem: Caches out of sync if instance 1 updates
```

### Solution: Redis (Recommended)

```
Server Instance 1               Server Instance 2
├─ Local MemoryCache            ├─ Local MemoryCache
└─ Redis (distributed)          └─ Redis (distributed)

Benefit: Single source of truth
```

---

## Caching Anti-Patterns (Currently Present)

### Anti-Pattern 1: Static MongoDB Connection

```csharp
private static IMongoDatabase _db;  // Shared cache connection

// Problem: Not reloaded, cache doesn't refresh
```

### Anti-Pattern 2: Unbounded Cache

```csharp
// MemoryCache has no size limit
_memCache.AddOrUpdateCache(key, largeObject, timeout);
// Could consume all available memory
```

### Anti-Pattern 3: No Cache Warming

```csharp
// App starts with cold cache
// First request slower than necessary
// Could pre-load common queries on startup
```

### Anti-Pattern 4: Cache Stampede Risk

```
Many requests → Cache expires
All see miss → All query database
Database overloaded
```

**Mitigation:** Probabilistic early expiration (not implemented)

---

## Cache Configuration

### MemoryCache Initialization

```csharp
services.AddSingleton<IMemCache, MemCache>();
```

**No Configuration Options:**
- ✗ Max size limit
- ✗ Eviction policy
- ✗ Compression
- ✗ Serialization format

---

## Monitoring & Observability

### Missing Metrics

```csharp
// Not currently tracked
var hitRate = CacheHits / (CacheHits + CacheMisses);
var averageHitTime = /* not measured */;
var cacheSize = /* not measured */;
var evictionCount = /* not tracked */;
```

### What Should Be Monitored

```
Per Cache Key:
├─ Hit/miss ratio
├─ Access frequency
├─ Memory size
└─ Expiration rate

Per Service:
├─ Total cache size
├─ Hit rate
├─ Eviction count
└─ Memory pressure

Alerts:
├─ Cache size > 80% memory
├─ Hit rate < 50% (poor cache quality)
└─ Eviction spike (thrashing)
```

---

## Caching in Different Scenarios

### Scenario 1: API Request

```
GET /api/clients
  ├─ Check MemoryCache: clients.all
  ├─ Hit → Return in 1ms
  └─ Miss → Query DB, cache, return in 100ms
```

### Scenario 2: Background Service

```
RabbitMQ Message: UpdateClientConfig
  ├─ Check cache: config.template.123
  ├─ Hit → Update existing
  └─ Miss → Load from DB, update
```

### Scenario 3: WebSocket Update

```
SignalR Message: ClientUpdated
  ├─ Invalidate cache: clients.123
  ├─ Delete: clients.all
  └─ Broadcast: via WebSocket to subscribed clients
```

---

## Caching Best Practices

### Currently Implemented ✓

- TTL-based expiration
- Event-driven invalidation
- Lazy-load pattern
- Async invalidation via RabbitMQ

### Should Implement ✗

- Cache warming on startup
- Size limit enforcement
- Hit rate monitoring
- Distributed caching
- Compression for large objects
- Serialization strategy
- Cache coherence protocol

---

## Future Cache Improvements

### Phase 1: Visibility
1. Add cache hit/miss metrics
2. Monitor memory usage
3. Track eviction frequency

### Phase 2: Resilience
1. Implement cache warming
2. Add circuit breaker
3. Handle cache failures gracefully

### Phase 3: Scalability
1. Migrate to distributed cache (Redis)
2. Implement cache-aside pattern consistently
3. Add cache versioning

### Phase 4: Performance
1. Add compression for large objects
2. Implement probabilistic early expiration
3. Add predictive cache pre-loading

---

## Summary

| Aspect | Status | Notes |
|--------|--------|-------|
| Provider | MemoryCache | Built-in, simple |
| Expiration | TTL-based | Per-entry timeout |
| Invalidation | Event-driven | Via RabbitMQ |
| Distribution | Single-instance | Doesn't scale |
| Monitoring | Missing | No visibility |
| Limits | Unbounded | Memory risk |
| Hit Rate | ~84% | Estimated |
| Performance | 50-250x faster | vs DB query |

