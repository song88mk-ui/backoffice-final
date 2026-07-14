# Caching

Repository-specific knowledge for the Backoffice caching system — `IMemCache`, cache key conventions, TTL choices, and the mandatory distributed invalidation via RabbitMQ. Apply this when adding cache to a new query, fixing stale data bugs, or tracing cache invalidation flows.

---

## Cache layers

```
Layer 1  Browser HTTP cache (controlled by response headers — not managed in this repo)
Layer 2  IMemCache (in-process MemoryCache, one instance per running server)
Layer 3  MongoDB Sportsbook collections (distributed read cache for fixture/market data)
Layer 4  SQL Server (source of truth — all layers derive from here)
```

The Backoffice codebase primarily interacts with **Layer 2 (IMemCache)**. Layer 3 is managed indirectly via RabbitMQ messages consumed by `Backoffice.Utility`.

## IMemCache interface

Defined in `Backoffice.Shared`. All repositories receive it via constructor injection:

```csharp
private readonly IMemCache _memCache;
```

Available methods:

```csharp
// Lazy-load with fallback: returns from cache or calls repositoryCall() and caches result
T GetCache<T>(string key, Func<T> repositoryCall, int expiration);  // expiration in hours

// Simple get — returns default(T) if missing
T GetCache<T>(string key);

// Set with TTL in hours
void AddOrUpdateCache<T>(string key, T cacheObject, int cacheTimeout);

// Set with absolute expiration
void AddOrUpdateCache<T>(string key, T cacheObject, DateTimeOffset date);

// Get with boolean hit indicator (used for long values)
KeyValuePair<bool, long> GetCache(string key);

// Delete a single key
void DeleteCache(string key);
```

## Cache key naming convention

```
{entity}.{operation}.{identifier}
```

All lowercase, dot-separated, minimum identifier needed:

```
clients.all                          full client list
clients.active                       active clients only
cc:{clientId}                        client config (short form — matches existing usage)
config.template.{templateId}         single template
config.templates.all                 all templates
config.margin.{clientId}_{marketId}  per-market margin
fixture.{fixtureId}                  single fixture
fixture.{fixtureId}.{langCode}       fixture with translation
league.{leagueId}.{langCode}         league with translation
sport.{sportId}                      sport lookup
```

The `cc:` prefix is legacy short form — preserve it in keys that already use it, but use the full form for new keys.

## TTL guidelines

| Data | TTL | Reason |
|------|-----|--------|
| Config templates | 24 h | Rarely changed |
| Sports / leagues | 24 h | Reference data, stable |
| Client list | 1 h | Changes on client add/update |
| Client bet settings | 1 h | Mutations trigger invalidation |
| Fixture data | 1 h | Status can change but OE pushes invalidation |
| Translated names | 1 h | Translation edits trigger invalidation |
| Activity log results | 15 min | Audit data, changes frequently |

For data that changes via explicit mutations (not external events), prefer **event-driven invalidation** over short TTLs — let the cache live longer and delete it on write.

## Lazy-load pattern (preferred for reads)

```csharp
var templates = _memCache.GetCache<List<ConfigTemplate>>(
    "config.templates.all",
    () => _backofficeContext.ConfigTemplates
              .AsNoTracking()
              .Include(t => t.Margins)
              .ToList(),          // Note: sync lambda inside GetCache<T>
    expiration: 24);
```

The `Func<T>` passed to `GetCache` is synchronous — the `IMemCache` implementation calls it on cache miss. Do not `await` inside this lambda.

## Invalidation — always two steps

Every cache delete must pair a local delete with a RabbitMQ broadcast:

```csharp
// Step 1 — local instance
_memCache.DeleteCache("config.template.123");

// Step 2 — broadcast to all instances
await _rabbitMqRepository.Publish(new OnDeleteCacheMessage
{
    CacheKey = "config.template.123",
    ClientId = clientId
}, region: client.RegionCode);
```

**Never do only Step 1.** In multi-instance deployments, other servers retain their cached copy until the RabbitMQ message arrives.

## Consumers of OnDeleteCacheMessage

| Project | Worker | Action |
|---------|--------|--------|
| `Backoffice.Utility` | `Worker.cs` | Calls `_memCache.DeleteCache(message.CacheKey)` |
| `Backoffice.WebSocket.Service` | `Worker.cs` | Calls `_memCache.DeleteCache(message.CacheKey)` |

If a new project is added that uses `IMemCache`, its `Worker.cs` must also handle `OnDeleteCacheMessage`.

## Pattern: invalidate on mutation

```csharp
public async Task<BaseResponse<bool>> UpdateConfigTemplateAsync(UpdateConfigTemplateRequest request)
{
    try
    {
        var template = await _backofficeContext.ConfigTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == request.TemplateId);

        if (template == null)
            return new BaseResponse<bool> { StatusCode = StatusCode.NotFound };

        template.TemplateName = request.TemplateName;
        await _backofficeContext.SaveChangesAsync();

        // Invalidate specific key
        var cacheKey = $"config.template.{request.TemplateId}";
        _memCache.DeleteCache(cacheKey);

        // Invalidate list key too
        _memCache.DeleteCache("config.templates.all");

        // Broadcast both
        await _rabbitMqRepository.Publish(new OnDeleteCacheMessage
        {
            CacheKey = cacheKey, ClientId = request.ClientId
        }, region: "HK");
        await _rabbitMqRepository.Publish(new OnDeleteCacheMessage
        {
            CacheKey = "config.templates.all", ClientId = request.ClientId
        }, region: "HK");

        return new BaseResponse<bool> { Result = true };
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to update ConfigTemplate {TemplateId}: {Exception}",
            request.TemplateId, ex);
        return new BaseResponse<bool> { StatusCode = StatusCode.Error };
    }
}
```

## Common stale-cache bugs

| Symptom | Root cause | Fix |
|---------|-----------|-----|
| UI shows old data after update | Mutation deletes cache locally but doesn't publish | Add `Publish(OnDeleteCacheMessage)` |
| Other service shows old data | `OnDeleteCacheMessage` handler missing in that service's `Worker.cs` | Add `case` block in consumer |
| Cache never populates after delete | Lazy-load fallback returns `null` and IMemCache skips caching null | Check fallback returns a non-null default |
| Cache key mismatch | Writer uses `"config.template.123"`, reader uses `"configtemplate123"` | Standardise key to `{entity}.{operation}.{id}` |
