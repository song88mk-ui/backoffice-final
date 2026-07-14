# /cache

Add, fix, or trace caching logic in the Backoffice solution using `IMemCache` and RabbitMQ-based distributed invalidation.

## Usage

```
/cache $ARGUMENTS
```

`$ARGUMENTS` examples:
- `add caching to BackofficeRepository.GetClientsAsync`
- `trace cache invalidation for client margin update`
- `fix stale cache after UpdateConfigTemplateAsync`
- `review all cache keys in BackofficeRepository.cs`
- `list what gets invalidated when a client is updated`

## Caching architecture

```
Browser (HTTP headers)
    ↓
Application IMemCache (in-process, per server instance)
    ↓
MongoDB (distributed read cache for Sportsbook data)
    ↓
SQL Server (source of truth)
```

**Important:** `IMemCache` is in-process. When running multiple API instances, each has its own cache. Invalidation must be broadcast via RabbitMQ so all instances clear the key.

## IMemCache patterns

### Pattern 1 — Lazy load with auto-populate (preferred for reads)
```csharp
var result = _memCache.GetCache<List<ConfigTemplate>>(
    "config.templates.all",                                    // cache key
    () => _backofficeContext.ConfigTemplates                   // fallback loader
              .AsNoTracking()
              .ToList(),
    expiration: 1);                                            // hours
```

### Pattern 2 — Explicit set (after writes)
```csharp
_memCache.AddOrUpdateCache("config.template.123", template, cacheTimeout: 24);
```

### Pattern 3 — Get only (no auto-populate)
```csharp
var cached = _memCache.GetCache<ClientList>("clients.all");
if (cached == null)
{
    // load from DB
}
```

### Pattern 4 — Delete
```csharp
_memCache.DeleteCache("clients.all");
```

## Cache key naming convention

```
{entity}.{operation}.{identifier}

Examples:
clients.all
clients.active
config.template.123
config.margin.{clientId}_{marketId}
cc:{clientId}           ← short form used in some RabbitMQ keys
fixture.{fixtureId}
```

Rules:
- All lowercase.
- Use dots as separators.
- Include the minimum identifier needed to make the key unique.
- Avoid cryptic abbreviations except where the existing codebase already uses them (e.g. `cc:`).

## Distributed invalidation (required on every mutation)

When a mutation clears a cache key, it must **also** broadcast via RabbitMQ so other server instances clear their copy:

```csharp
// Step 1 — clear locally
_memCache.DeleteCache("config.template.123");

// Step 2 — broadcast to other instances
await _rabbitMqRepository.Publish(new OnDeleteCacheMessage
{
    CacheKey = "config.template.123",
    ClientId = clientId
}, region: "HK");
```

The broadcast is handled by:
- `Backoffice.Utility/Worker.cs` — receives `OnDeleteCacheMessage`, calls `DeleteCache` on its local cache
- `Backoffice.WebSocket.Service/Worker.cs` — same

**Never** invalidate only locally. Always pair with a `Publish`.

## TTL guidelines

| Data type | Recommended TTL |
|-----------|----------------|
| Config templates | 24 hours |
| Client configuration | 1 hour |
| Client list | 1 hour |
| Fixture/market data | 1 hour |
| Sports/leagues lookup | 24 hours |
| User session data | Session lifetime |

Volatile data (changes frequently) → shorter TTL or event-driven invalidation.
Stable data (lookup tables) → longer TTL.

## Adding caching to an existing method

### Before
```csharp
public async Task<BaseResponse<List<ConfigTemplate>>> GetConfigTemplatesAsync()
{
    try
    {
        var templates = await _backofficeContext.ConfigTemplates
            .AsNoTracking()
            .ToListAsync();
        return new BaseResponse<List<ConfigTemplate>> { Result = templates };
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to get config templates: {Exception}", ex);
        return new BaseResponse<List<ConfigTemplate>> { StatusCode = StatusCode.Error };
    }
}
```

### After
```csharp
public async Task<BaseResponse<List<ConfigTemplate>>> GetConfigTemplatesAsync()
{
    try
    {
        var templates = _memCache.GetCache<List<ConfigTemplate>>(
            "config.templates.all",
            () => _backofficeContext.ConfigTemplates.AsNoTracking().ToList(),
            expiration: 24);
        return new BaseResponse<List<ConfigTemplate>> { Result = templates };
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to get config templates: {Exception}", ex);
        return new BaseResponse<List<ConfigTemplate>> { StatusCode = StatusCode.Error };
    }
}
```

## Output format

For add-caching tasks: show the before/after diff and the new cache key string.
For trace tasks: list every place the key is set, read, and deleted, with file paths and line numbers.
For fix tasks: show the corrected mutation method with both local delete and RabbitMQ publish.
For review tasks: produce a table of all cache keys found, their TTLs, and whether distributed invalidation is wired correctly.
