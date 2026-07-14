# /performance

Identify and fix performance issues in a specific file, method, or feature area of the Backoffice solution.

## Usage

```
/performance $ARGUMENTS
```

`$ARGUMENTS` examples:
- `BackofficeRepository.GetClientsAsync`
- `Backoffice.API/Repositories/TradingRepository.cs`
- `client configuration load time`
- `RabbitMQ message throughput`

## Analysis dimensions

### 1. N+1 Query Detection
Scan for patterns where a query inside a loop triggers per-row database calls:

```csharp
// BAD — N+1
var clients = await _context.Clients.ToListAsync();
foreach (var client in clients)
{
    var config = await _context.ClientBetSettings
        .FirstOrDefaultAsync(c => c.ClientId == client.Id);  // One query per client
}

// GOOD — Single query with Include
var clients = await _context.Clients
    .AsNoTracking()
    .Include(c => c.BetSettings)
    .ToListAsync();
```

### 2. Missing AsNoTracking()
Every read-only query must use `.AsNoTracking()`. EF Core's change tracker adds overhead for data that is never modified.

### 3. HttpClient Instantiation
Look for `new HttpClient()` inside methods or constructors of non-singleton classes:

```csharp
// BAD — new client per request, exhausts sockets
public async Task<string> CallExternalApi()
{
    var client = new HttpClient();
    return await client.GetStringAsync(url);
}

// GOOD — injected, pooled
public class XxxService
{
    private readonly HttpClient _httpClient;
    public XxxService(HttpClient httpClient) { _httpClient = httpClient; }
}
// Registered in Startup.cs:
// services.AddHttpClient<XxxService>();
```

### 4. Missing or Incorrect Caching
Check for expensive queries (joins, aggregates, full table scans) that lack a caching layer:

```csharp
// BAD — hits DB every request
var templates = await _context.ConfigTemplates.AsNoTracking().ToListAsync();

// GOOD — cached for 1 hour
var templates = _memCache.GetCache<List<ConfigTemplate>>(
    "config.templates.all",
    () => _context.ConfigTemplates.AsNoTracking().ToList(),
    expiration: 1);
```

### 5. Synchronous Calls in Async Context
Look for `.Result`, `.Wait()`, or blocking `Task.Run` in async methods — these cause thread-pool starvation:

```csharp
// BAD
var result = _repository.GetClientsAsync().Result;

// GOOD
var result = await _repository.GetClientsAsync();
```

### 6. Over-fetching
Queries that select all columns when only a subset is needed:

```csharp
// BAD — loads entire entity
var client = await _context.Clients.AsNoTracking()
    .FirstOrDefaultAsync(c => c.Id == id);
var name = client.Name;

// GOOD — projects only what's needed
var name = await _context.Clients.AsNoTracking()
    .Where(c => c.Id == id)
    .Select(c => c.Name)
    .FirstOrDefaultAsync();
```

### 7. Message Serialization
JSON serialization/deserialization on the hot path. Flag if `JsonSerializer.Serialize` is called inside a tight loop without caching the result.

### 8. MongoDB Queries Without Filters
Any MongoDB `FindAsync` call without a filter predicate performs a full collection scan:

```csharp
// BAD — full scan
var all = await collection.FindAsync(FilterDefinition<Fixture>.Empty);

// GOOD — indexed filter
var filter = Builders<Fixture>.Filter.Eq(f => f.FixtureId, fixtureId);
var result = await collection.FindAsync(filter, new FindOptions
{
    ReadPreference = ReadPreference.PrimaryPreferred
});
```

## Fix guidelines

- Apply only the fixes within the analyzed scope.
- Do not change method signatures.
- Preserve error handling (try/catch wrappers must stay).
- After adding caching, ensure cache invalidation is also wired.
- After fixing HttpClient, update DI registration in Startup.cs.

## Output format

For each issue found:
1. **Issue** — one sentence describing the problem and its impact.
2. **Location** — `File:Line`.
3. **Before/After diff** — complete changed block.

End with a summary table:

| Issue | Location | Estimated Impact |
|-------|----------|-----------------|
| N+1 query | XxxRepository.cs:142 | High — 1 query per row |
| Missing AsNoTracking | XxxRepository.cs:98 | Medium — change-tracker overhead |
