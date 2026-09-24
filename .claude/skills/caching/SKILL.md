---
name: caching
description: How caching works in backoffice-final: the in-process IMemCache wrapper, the cache keys actually in use, and the r.oe.cachesync.* messages that tell downstream services to drop their caches. Use when adding a cache, chasing stale data, or tracing a cache-sync publish.
---

# Caching

Repository-specific knowledge for caching in backoffice-final: the in-process `IMemCache` wrapper, the keys actually in use, and how Backoffice tells downstream platform and OddsEngine services to drop their caches over RabbitMQ. Apply this when adding a cache to a query, chasing a stale-data bug, or tracing a `r.oe.cachesync.*` publish.

---

## Two different things called "cache" here

1. **Local `IMemCache`**: a thin wrapper over `System.Runtime.Caching.MemoryCache.Default` inside each Backoffice process. Nothing invalidates it from outside the process.
2. **Downstream cache sync**: Backoffice publishes `OnDeleteCacheMessage` (plus `OnTranslationChangeMessage` and `OnWebPropsChangeMessage`) on `r.oe.cachesync.*` so that *other repos'* services purge *their* caches. No Backoffice service consumes these messages: the only Backoffice queue bindings are `r.bo.util.#`, `r.bo.config.#` and the WebSocket.Service odds/betstop/notification keys (their `appsettings.json` `RoutingKey`).

Do not confuse the two. A `CacheKey` inside an `OnDeleteCacheMessage` is a downstream key, not an `IMemCache` key.

## IMemCache interface

`Backoffice.Shared/Utilities/IMemCache.cs:6-18`:

```csharp
void AddOrUpdateCache<T>(string key, T cacheObject, int cacheTimeout);          // hours
void AddOrUpdateCache<T>(string key, T cacheObject, DateTimeOffset date);       // absolute expiry
T GetCache<T>(string key, Func<T> repositoryCall, int expiration) where T : class;
T GetCache<T>(string key) where T : class;
KeyValuePair<bool, long> GetCache(string key);
void DeleteCache(string key);
```

Implementation facts (`Backoffice.Shared/Utilities/MemCache.cs`):

- All entries use `AbsoluteExpiration`; the `int` overload means hours (`DateTimeOffset.Now.AddHours(cacheTimeout)`, :14). There is no sliding expiration.
- `GetCache(key, repositoryCall, expiration)` calls the factory on a miss and caches the result only if it is non-null (:26-39). A null result is re-fetched on every call.
- `GetCache(string key)` returning `KeyValuePair<bool, long>` hard-casts the stored object (:45); it throws if the key holds anything else. No current caller uses this overload. The generic `GetCache<T>` overloads also use a hard `(T)` cast (:28, :50), so reading a key with the wrong type throws `InvalidCastException` instead of returning null.
- There is no locking or stampede protection; concurrent misses all call the factory.

## Where it is registered

`IMemCache` is a singleton in three processes, each with its own `MemoryCache.Default`:

| Project | Registration |
|---|---|
| Backoffice.API | `Startup.cs:124` |
| Backoffice.Web | `Program.cs:36` |
| Backoffice.WebSocket.Service | `Program.cs:37` |

Config.Service, Utility and WebSocket.API do not register it. The API tests pass `new Mock<IMemCache>().Object`. Because each replica has its own memory, a write on one API pod does not clear the entry on another pod; the entry lives until its TTL.

## Keys actually in use

There is no key convention. Keys are ad hoc strings:

| Key | Where | TTL |
|---|---|---|
| `tlo` | API `SportsbookRepository.cs:822` (TierLeagueOrder) | 24 h |
| `sportLookUp-{langCode}` | API `SportsbookRepository.cs:1274` | 24 h (:1302) |
| `sp-tr-{LANG}` | API `BackOfficeRepository.cs:2394` | 1 h |
| `tr-{LANG}` | API `BackOfficeRepository.cs:2567-2599` | 24 h |
| `as:t:{userToken}` | Web `Services/AuthenticationService.cs:122-148` | 30 s |
| `metabase-clients:{userId}` | Web written in `Pages/SBC/Dashboard.razor.cs:126` and `Pages/Administration/Dashboard.razor.cs:127`, read in `Program.cs:89` | 12 h |
| `mtpp`, `acnm` | WebSocket.Service `Repositories/SportsbookRepository.cs:35-58` (read at :602-603), loaded once at startup from `Worker.cs:97-99`; failures are swallowed by empty `catch` blocks | 1 year |

Several other call sites are commented out (`boci` at `BackOfficeRepository.cs:2333,2341`, `tr` at :2481, TradingRepository sports tree at `TradingRepository.cs:66,89`). If you add a key, grep for the prefix first so you do not collide with an existing one.

## Lazy-load pattern

Real synchronous example, `Backoffice.API/Repositories/SportsbookRepository.cs:820-823`:

```csharp
private List<TierLeagueOrder> GetTierLeagueOrder()
{
    return _memCache.GetCache($"tlo", () => _sportsbookContext.TierLeagueOrder.ToList(), 24);
}
```

**Async lambda trap.** Two call sites pass an `async` lambda, so `T` becomes `Task<...>` and the *Task* is cached:

```csharp
// Backoffice.API/Repositories/SportsbookRepository.cs:1273-1274
public async Task<BaseResponse<SportsLookupResponse>> GetSportLookup(string langCode = "EN")
    => await _memCache.GetCache($"sportLookUp-{langCode}", async () =>
```

```csharp
// Backoffice.API/Repositories/BackOfficeRepository.cs:2394-2396
var cache = _memCache.GetCache($"sp-tr-{langCode.ToString().ToUpper()}", async () => await _tradingRepository.GetSportLookup(langCode.ToString().ToUpper()), 1);
return cache.Result?.Result?.SportLookup ?? null;
```

Consequences: a Task that faults (the `sportLookUp` lambda rethrows, :1296-1300) is non-null and stays cached for the full TTL, and `sp-tr-` blocks on `.Result`. Do not copy this pattern; for async sources use the explicit get / fetch / `AddOrUpdateCache` shape used for `tr-{LANG}` (`BackOfficeRepository.cs:2565-2599`).

## Downstream cache sync over RabbitMQ

API mutations that affect data cached in platform-final or odds-engine-2 publish an `OnDeleteCacheMessage` (`Backoffice.Shared/OutgoingEngineMessages/OnDeleteCacheMessage.cs`: `ClientId` (Guid), `ClientCode`, `CacheType`, `CacheKey`, `CacheKeys`, `LanguageCode`, `PurgeAll`). Real example, `Backoffice.API/Repositories/SportsbookRepository.cs:249-261`:

```csharp
await _rabbitMqRepository.Publish(new MessageRequest
{
    Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
    {
        CacheType = CacheType.MarketSortOrder,
        ClientId = request.ClientId,
        ClientCode = string.Empty,
        CacheKey = string.Empty
    }),
    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
    MessageType = "OnMarketSortOrderChange",
    RoutingKey = $"r.oe.cachesync.onmarketsortorderchange"
}, region);
```

Other publishers of this shape: `SportsbookRepository.cs` (market and sport sort order, several sites), `PlatformRepository.cs:97` (`onclientchange`) and :672 (`onparlaybonuschange`), `BackOfficeRepository.cs:3759` (`onstatictranslationchange`). `BackOfficeRepository.cs:3743` publishes `OnTranslationChangeMessage` on `r.oe.cachesync.ontranslationchange`, and `WebPropsRepository.cs:181` publishes `OnWebPropsChangeMessage` on `r.oe.cachesync.onwebpropschange.{clientcode}`.

`Publish` does not talk to RabbitMQ directly: `RabbitMQRepository.Publish` POSTs the `MessageRequest` to a broker HTTP service at `v1/broker/publish` (`Backoffice.API/Repositories/RabbitMQRepository.cs:32-37`), selected by `region`.

`CacheType` to message type mapping is in `BackOfficeRepository.GetMessageType` (`BackOfficeRepository.cs:3768-3785`); an undefined `CacheType` value throws `ArgumentOutOfRangeException`. `POST cachepreload` (`Controllers/Backoffice/ConfigController.cs:301-303`, `[AllowAnonymous]`) validates `CacheKey`/`CacheKeys` unless `PurgeAll` is set, then republishes the `OnDeleteCacheMessage` through `CachePreload` (`BackOfficeRepository.cs:2545-2561`).

The `cc:{CLIENTID}` key in `PlatformRepository.cs:93` is a platform-final key (for example `platform-final/Platform/Platform.ClientSportsbook.API/ActionFilters/AuthorizationFilter.cs:90`), not a Backoffice one.

Downstream consumer: `platform-final/Platform/Platform.CacheSync.Service` binds `r.oe.cachesync.#` (`appsettings.json:18`). It keeps its own copy of `OnDeleteCacheMessage` and `CacheType` (`Platform.Shared/MessageModels/OnDeleteCacheMessage.cs`, `Platform.Shared/Enums/CacheType.cs`); they currently have the same fields and enum values (only difference: Platform's `ClientCode` defaults to `string.Empty`, Backoffice's to null). Change both or neither.

## Common stale-cache causes

| Symptom | Likely cause |
|---|---|
| One API pod serves old data after an edit | `IMemCache` is per process; nothing clears other replicas before TTL |
| Sport lookup errors persist for hours | Faulted `Task` cached under `sportLookUp-{lang}` (24 h) |
| Player site keeps old config after a BO save | The `r.oe.cachesync.*` publish failed silently: `Publish` returns `StatusCode.Error` instead of throwing and callers ignore the result (see messaging.md) |
| Downstream purge does nothing | `CacheType` or message shape drifted from platform-final's copy |
