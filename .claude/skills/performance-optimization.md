# Performance Optimization

Repository-specific performance knowledge for the Backoffice solution — known bottlenecks, proven fixes, and where to look first. Apply this when diagnosing slowness, reviewing query efficiency, or adding caching to an existing flow.

---

## Known production bottlenecks

| Bottleneck | Location | Impact | Fix approach |
|-----------|----------|--------|-------------|
| `HttpClient` created per request | Web service classes in `Backoffice.Web/Services/` | Socket exhaustion under load | Register with `AddHttpClient<T>()` in Startup.cs |
| N+1 queries in activity log | `BackofficeRepository` — log queries that join per-row | Slow dashboard load | Batch with `.Include()` or grouped fetch |
| In-process cache not shared | All services using `IMemCache` | Cold-start latency on new instances | Cannot fix without distributed cache; mitigate with event-driven invalidation |
| Static MongoDB init not thread-safe | `SportsbookRepository` | Potential duplicate init at startup | Use `Lazy<IMongoDatabase>` for thread-safe init |
| Fire-and-forget RabbitMQ publish | All repositories | Undetected failures cause stale state | Await publish and handle exceptions |

## AsNoTracking — highest ROI fix

Every read query without `.AsNoTracking()` allocates EF Core change-tracking objects. For read-heavy endpoints (client list, config templates, fixture lookups), this adds measurable CPU and memory overhead:

```csharp
// Before — unnecessary tracking
var clients = await _context.Clients.ToListAsync();

// After — no change tracking overhead
var clients = await _context.Clients.AsNoTracking().ToListAsync();
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

Pattern:
```csharp
var templates = _memCache.GetCache<List<ConfigTemplate>>(
    "config.templates.all",
    () => _backofficeContext.ConfigTemplates.AsNoTracking().Include(t => t.Margins).ToList(),
    expiration: 24);
```

After adding caching, always wire the corresponding invalidation in the mutation method.

## N+1 detection and fix

```csharp
// BAD — N+1: one DB query per client to load settings
var clients = await _context.Clients.AsNoTracking().ToListAsync();
foreach (var c in clients)
{
    var settings = await _context.ClientBetSettings
        .Where(s => s.ClientId == c.ClientId).ToListAsync();
}

// GOOD — single query: Include loads settings in one JOIN
var clients = await _context.Clients
    .AsNoTracking()
    .Include(c => c.BetSettings)
    .ToListAsync();

// GOOD — when Include is not available (cross-context): batch by IDs
var clientIds = clients.Select(c => c.ClientId).ToList();
var settings = await _context.ClientBetSettings
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
var keys = Builders<Fixture>.IndexKeys.Ascending(f => f.LeagueId);
await collection.Indexes.CreateOneAsync(
    new CreateIndexModel<Fixture>(keys,
        new CreateIndexOptions { Name = "idx_fixtures_leagueid", Background = true }));
```

3. Use `.Projection` to return only needed fields from large documents.

## Async correctness — thread-pool starvation

`.Result` and `.Wait()` on async calls block a thread-pool thread and can cause cascading starvation under load:

```csharp
// BAD — blocks thread
var result = _repository.GetClientsAsync().Result;
var count = _context.Clients.CountAsync().Result;

// GOOD — frees thread while waiting
var result = await _repository.GetClientsAsync();
var count = await _context.Clients.CountAsync();
```

Search for `.Result` and `.Wait()` in repository and controller files — each instance is a starvation risk.

## Pagination — mandatory for unbounded result sets

Activity logs, historical data, and any query where the row count is not bounded by a foreign key filter must use pagination:

```csharp
var results = await _context.ActivityLogs
    .AsNoTracking()
    .Where(a => a.ClientId == clientId)
    .OrderByDescending(a => a.Timestamp)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

Default page size: 50. Maximum page size: 200. Never return an unbounded list from an endpoint that could return thousands of rows.

## Projection — avoid over-fetching

For list endpoints that only display summary data, project to a DTO instead of loading the full entity:

```csharp
// BAD — loads all columns including blobs/large fields
var clients = await _context.Clients.AsNoTracking().ToListAsync();

// GOOD — loads only what the list view needs
var clients = await _context.Clients
    .AsNoTracking()
    .Select(c => new ClientSummaryModel
    {
        ClientId = c.ClientId,
        ClientName = c.ClientName,
        Status = c.Status,
        RegionCode = c.RegionCode
    })
    .ToListAsync();
```

## Query timeout monitoring

All queries have a 180-second timeout configured in Startup.cs. Any query that regularly takes over 5 seconds in production should be:
1. Logged with `LogWarning` including query parameters
2. Reviewed for missing indexes or N+1 patterns
3. Added to the caching layer if the data is read-heavy

Do not raise the 180-second timeout — diagnose and fix the query instead.
