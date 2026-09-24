---
name: performance-optimization
description: Known performance bottlenecks in the Backoffice solution and the fixes that apply here: HttpClient creation, AsNoTracking, N+1 queries, caching, pagination and query timeouts. Use when diagnosing slowness, reviewing query efficiency, or adding caching to an existing flow.
---

# Performance Optimization

Repository-specific performance knowledge for the Backoffice solution — known bottlenecks, proven fixes, and where to look first. Apply this when diagnosing slowness, reviewing query efficiency, or adding caching to an existing flow.

---

## Known production bottlenecks

| Bottleneck | Location | Impact | Fix approach |
|-----------|----------|--------|-------------|
| `HttpClient` created per call | `Backoffice.API` repositories: `SportsbookRepository.cs:1421`, `RabbitMQRepository.cs:85`, `PlatformRepository.cs:1008`, `TradingRepository.cs:2417,2435` | Socket exhaustion under load | Register with `AddHttpClient<T>()` in `Startup.cs` |
| N+1 queries in activity log | `BackofficeRepository` — log queries that join per-row | Slow dashboard load | Batch with `.Include()` or grouped fetch |
| In-process cache not shared | All services using `IMemCache` | Cold-start latency on new instances | Cannot fix without distributed cache; mitigate with event-driven invalidation |
| Static MongoDB init not thread-safe | `SportsbookRepository.cs:29`, `TradingRepository.cs:28` | Potential duplicate init at startup | Use `Lazy<IMongoDatabase>` for thread-safe init |
| Fire-and-forget RabbitMQ publish | All repositories | Undetected failures cause stale state | Await publish and handle exceptions |

## AsNoTracking — highest ROI fix

Every read query without `.AsNoTracking()` allocates EF Core change-tracking objects. For read-heavy endpoints (client list, config templates, fixture lookups), this adds measurable CPU and memory overhead:

```csharp
// Before — unnecessary tracking
var clients = await _backofficeContext.ClientInformation.ToListAsync();

// After — no change tracking overhead
var clients = await _backofficeContext.ClientInformation.AsNoTracking().ToListAsync();
```

Scan `BackofficeRepository.cs` for any `.FirstOrDefaultAsync()` or `.ToListAsync()` call not preceded by `.AsNoTracking()`.

## HttpClient factory — correct registration

```csharp
// BAD — socket leak, one client per request
public class ExternalService
{
    public async Task<string> CallApi()
    {
        var client = new HttpClient();  // New socket every time
        return await client.GetStringAsync(url);
    }
}

// GOOD — pooled, injected
public class ExternalService
{
    private readonly HttpClient _httpClient;
    public ExternalService(HttpClient httpClient) { _httpClient = httpClient; }
}

// In Startup.cs:
services.AddHttpClient<ExternalService>(client =>
{
    client.BaseAddress = new Uri(config["ExternalApi:BaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

## Caching expensive queries — where to add it

Priority targets for caching (not yet cached or partially cached):
1. Config template list — rarely changes, queried on every client page load
2. Sports and leagues lookup — reference data, changes only with new competitions
3. Client list — queried by UI on navigation, changes only on client add/update
4. Fixture translations — stable until explicitly edited

Pattern (illustrative; the key is made up, `GetCache<T>(string key, Func<T> repositoryCall, int expiration)` is the real signature in `Backoffice.Shared/Utilities/IMemCache.cs`). `ConfigTemplate` has no margins navigation; margins are the separate `ConfigTemplateMargin` DbSet:
```csharp
var templates = _memCache.GetCache<List<ConfigTemplate>>(
    "example.configtemplates",
    () => _backofficeContext.ConfigTemplate.AsNoTracking().ToList(),
    24);
```

After adding caching, always wire the corresponding invalidation in the mutation method.

## N+1 detection and fix

```csharp
// Illustrative. ClientInformation has no navigation to ClientBetSetting, so batch by IDs.
// BAD — N+1: one DB query per client to load settings
var clients = await _backofficeContext.ClientInformation.AsNoTracking().ToListAsync();
foreach (var c in clients)
{
    var settings = await _backofficeContext.ClientBetSetting
        .Where(s => s.ClientId == c.ClientId).ToListAsync();
}

// GOOD: batch by IDs, one query
var clientIds = clients.Select(c => (long)c.ClientId).ToList();
var settings = await _backofficeContext.ClientBetSetting
    .AsNoTracking()
    .Where(s => clientIds.Contains(s.ClientId))
    .ToListAsync();
var settingsMap = settings.ToLookup(s => s.ClientId);
```

## MongoDB query performance

MongoDB full-collection scans are the equivalent of `SELECT *` with no WHERE clause. Before adding any new MongoDB query:

1. Confirm an index exists on the filter field.
2. If not, add a background index (does not block reads during build):

```csharp
var keys = Builders<Fixture>.IndexKeys.Ascending(f => f.LeId);
await collection.Indexes.CreateOneAsync(
    new CreateIndexModel<Fixture>(keys,
        new CreateIndexOptions { Name = "idx_fixtures_leagueid", Background = true }));
```

3. Use `.Projection` to return only needed fields from large documents.

## Async correctness — thread-pool starvation

`.Result` and `.Wait()` on async calls block a thread-pool thread and can cause cascading starvation under load:

```csharp
// BAD — blocks thread
var result = _backofficeRepository.GetClients().Result;
var count = _backofficeContext.ClientInformation.CountAsync().Result;

// GOOD — frees thread while waiting
var result = await _backofficeRepository.GetClients();
var count = await _backofficeContext.ClientInformation.CountAsync();
```

Search for `.Result` and `.Wait()` in repository and controller files — each instance is a starvation risk.

## Pagination — mandatory for unbounded result sets

Activity logs, historical data, and any query where the row count is not bounded by a foreign key filter must use pagination:

```csharp
// ActivityLog has no ClientId; filter on a real column such as Page or UserId
var results = await _backofficeContext.ActivityLog
    .AsNoTracking()
    .Where(a => a.Page == page)
    .OrderByDescending(a => a.TimeStamp)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

Default page size: 50. Maximum page size: 200. Never return an unbounded list from an endpoint that could return thousands of rows.

## Projection — avoid over-fetching

For list endpoints that only display summary data, project to a DTO instead of loading the full entity:

```csharp
// BAD — loads all columns including blobs/large fields
var clients = await _backofficeContext.ClientInformation.AsNoTracking().ToListAsync();

// GOOD — loads only what the list view needs (ClientSummaryModel is illustrative)
var clients = await _backofficeContext.ClientInformation
    .AsNoTracking()
    .Select(c => new ClientSummaryModel
    {
        ClientId = c.ClientId,
        Name = c.Name,
        StatusId = c.StatusId,
        Region = c.Region
    })
    .ToListAsync();
```

## Query timeout monitoring

All API DbContexts use `Custom:DbContextTimeout`, default 180 seconds (`Backoffice.API/Startup.cs:91-102`). Any query that regularly takes over 5 seconds in production should be:
1. Logged with `LogWarning` including query parameters
2. Reviewed for missing indexes or N+1 patterns
3. Added to the caching layer if the data is read-heavy

Do not raise the 180-second timeout — diagnose and fix the query instead.
