---
name: mongodb-best-practices
description: Facts about the read-only MongoDB Sportsbook catalogue as Backoffice uses it: collections, static client initialisation, read preference, filters and projections. Use when touching SportsbookRepository, TradingRepository or the classes in Backoffice.Shared/Data/Sportsbook/Collections/.
---

# MongoDB Best Practices

This page lists facts about how backoffice-final uses MongoDB. The `Sportsbook` Mongo database holds the fixture, market and selection catalogue and its lookup collections, and Backoffice only reads it. Use it when you touch `SportsbookRepository`, `TradingRepository`, or the classes in `Backoffice.Shared/Data/Sportsbook/Collections/`.

## Where Mongo is used

Only two classes in the solution talk to Mongo. Both are in `Backoffice.API/Repositories/`:

| Repository | Collections | Read preference |
|---|---|---|
| `SportsbookRepository` | `SportLookup`, `SegmentLookup`, `LeagueLookup`, `MarketLookup` | `PrimaryPreferred` (SportsbookRepository.cs:56-59) |
| `TradingRepository` | `Fixture`, `Market`, `Selection`; `SportLookup`, `SegmentLookup`, `LeagueLookup` (BO-144, for `GetAllLeagueLookup`) | `Secondary` for the first three, `PrimaryPreferred` for the lookups (the constructor of TradingRepository.cs) |

`Backoffice.Utility`, `Backoffice.Config.Service`, `Backoffice.WebSocket.Service`, `Backoffice.Web` and `Backoffice.WebSocket.API` have no Mongo code. The only match in `Backoffice.Web` is a comment in `EditBoostedOddsModal.razor.cs:130`. The driver is `MongoDB.Driver` 3.7.1, referenced from `Backoffice.Shared/Backoffice.Shared.csproj:19`. The only other file that uses the driver is the test `Backoffice.API.Tests/MarketSettlement/TradingRepositorySettlementTests.cs`.

Backoffice never writes to Mongo. The repo has no `InsertOne`, `ReplaceOne`, `UpdateOne`, `DeleteOne`, `BulkWrite` or index creation. The catalogue is written outside this repo: the `SportsbookRepository` classes in odds-engine-2 (`OE.Sportsbook.Service`, `OE.Job.Lookup`, `OE.Job.LiveStream`) do contain write calls. Do not add a write path here without first agreeing it with the owners of odds-engine-2.

## Connection pattern and known risk

The configuration key is `Custom:MongoDB-Sportsbook` (Backoffice.API/appsettings.json:97). The connection string there sets an unacknowledged write concern. That does not matter today because nothing writes, but it would matter if a write path were added.

Each repository keeps its own static `IMongoDatabase _db` (SportsbookRepository.cs:29, TradingRepository.cs:28) and its own static collection handles. The constructor sets them up the first time it runs. Both repositories are registered with `AddScoped` (Backoffice.API/Startup.cs:120-121), so the constructor runs on every request, but it only builds the client when `_db` is null.

```csharp
// Backoffice.API/Repositories/TradingRepository.cs:47-55
if (_db == null)
{
    var client = new MongoClient(_configuration["Custom:MongoDB-Sportsbook"]);
    _db = client.GetDatabase("Sportsbook");

    fixtureCollection = _db.GetCollection<Fixture>("Fixture").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
    marketCollection = _db.GetCollection<Market>("Market").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
    selectionCollection = _db.GetCollection<Selection>("Selection").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
}
```

`SportsbookRepository` uses the same pattern at SportsbookRepository.cs:51-60.

What this means:
- The `_db == null` check is not thread-safe. `_db` is assigned before the collection handles. A second request that arrives in that gap sees a non-null `_db`, skips the block, and can hit a null collection field. Two concurrent first requests can also each build a `MongoClient`. Do not add more static state on top of this pattern.
- The two repositories each create their own `MongoClient`.
- The database name is hard-coded as `"Sportsbook"` in both `GetDatabase` calls. The database named in the connection string is not used to pick the database.
- Tests construct `TradingRepository` with a dummy localhost URI. `MongoClient` connects lazily, so the constructor never dials out (Backoffice.API.Tests/MarketSettlement/TradingRepositorySettlementTests.cs:28-32).

## Read preference is set per collection

Read preference is set once, with `WithReadPreference` on the collection handle, and not on each query. New queries inherit it from the handle they use. `Fixture`, `Market` and `Selection` use `Secondary`, which can return stale data. That matters when a new read needs fresh data, for example a document another service has just written. In that case, create a separate handle instead of changing the shared one.

## Collections and document shapes

Field names in the documents are abbreviated. All seven collection classes have `[BsonIgnoreExtraElements]`, so fields written by other services that the class does not declare are dropped silently. The projection targets (`MarketFilter`, `SelectionFilter`) and the nested translation classes do not have that attribute.

| Collection | Class | `_id` | Key fields |
|---|---|---|---|
| `Fixture` | `Fixture` (Collections/Fixture.cs) | `string Id` | `long FiId`, `int SpId`, `int SegId`, `int LeId`, `int FiStId`, `int FiTyId`, `string PrFiId`, `string ClCo` |
| `Market` | `Market` (Collections/Market.cs) | `string Id` | `long MaId`, `long FiId`, `string MaTeId`, `int MaStId`, `string Li` |
| `Selection` | `Selection` (Collections/Selection.cs) | `string Id` | `long SeId`, `long MaId`, `long FiId`, `string SeTeId`, `string SeSp`, `decimal Od` |
| `SportLookup` | `SportLookup` | `int SpId` | `SpNa`, `IsMap` |
| `SegmentLookup` | `SegmentLookup` | `string SeLoId` | `SpId`, `SegId`, `SegNa`, `IsMap` |
| `LeagueLookup` | `LeagueLookup` | `string LeLoId` | `SpId`, `SegId`, `LeId`, `LeNa`, `IsMap` |
| `MarketLookup` | `MarketLookup` | `string MaLoId` | `SpId`, `MaTeId`, `MaTeNa`, `FiTyId`, `IsMap` |

Translation arrays are stored inside the catalogue documents:
- `Fixture` has `SpNaTr`, `SegNaTr`, `LeNaTr` and `FiNaTr` (Fixture.cs:45-48).
- `Market` has `MaNaTr` (Market.cs:35).
- `Selection` has `SeNaTr` (Selection.cs:43).

Each element's `[BsonId]` is `LaCo`, the language code.

Namespace quirk: the four lookup classes live in `Backoffice.Shared` but are declared in the namespace `OE.Shared.Data.Sportsbook.Collections` (for example LeagueLookup.cs:3). `Fixture`, `Market` and `Selection` use `Backoffice.Shared.Data.Sportsbook.Collections`. `SportsbookContext` and several `SQLEntities` classes also use `OE.Shared.*` namespaces. None of these come from the odds-engine-2 `OE.Shared` package, which no Backoffice project references.

## Filter construction

Existing code builds filters with `Builders<T>.Filter.Where(...)` lambdas. It uses `Filter.Empty` when no sport or segment is supplied:

```csharp
// Backoffice.API/Repositories/SportsbookRepository.cs:1308-1312, 1322
var leagueLookupFilter = Builders<LeagueLookup>.Filter.Empty;
if (sportId > 0 && segmentId > 0)
{
    leagueLookupFilter = Builders<LeagueLookup>.Filter.Where(c => c.SpId == sportId && c.SegId == segmentId);
}
var leagueLookup = await _leagueLookupCollection.Find(leagueLookupFilter).ToListAsync();
```

`GetSegmentLookup` (SportsbookRepository.cs:1358-1359) and `GetMarketLookup` (SportsbookRepository.cs:1391-1392) follow the same pattern.

`GetSportLookup` reads the whole `SportLookup` collection with `Find(_ => true)` (SportsbookRepository.cs:1272-1301). Its result is cached for 24 hours under the key `sportLookUp-{langCode}`; the expiry is in hours (Backoffice.Shared/Utilities/MemCache.cs:14). What `MemCache.GetCache` stores is the `Task` returned by the async lambda. The lambda's `catch` rethrows, so a failed Mongo read leaves a faulted task in the cache until the entry expires. Full scans are acceptable on the small lookup collections, but not on `Fixture`, `Market` or `Selection`.

Unverified: which indexes exist on the production collections. Nothing in this repo creates or documents them. Check with the odds-engine-2 owners before you filter the catalogue collections on a new field.

## Projection

`TradingRepository.GetManualMarketSettlementDetails` is the only method that reads the catalogue collections, and the only place that uses projection. It projects into the lighter `MarketFilter` and `SelectionFilter` classes. It uses `ElemMatch` to keep only the English translation element:

```csharp
// Backoffice.API/Repositories/TradingRepository.cs:1331-1333
var marketFilter = Builders<Market>.Filter.Where(c => c.MaId == marketId && c.FiId == fixtureId);
var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == LanguageCode.EN.ToString());
var market = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).FirstOrDefault();
```

The `Fixture` read in the same method (TradingRepository.cs:1319-1320) has no projection and loads the whole document. The method is `async`, but it calls the synchronous `Find(...).FirstOrDefault()` (TradingRepository.cs:1320, 1333, 1337). Use `FirstOrDefaultAsync()` in new code.

## Settlement depends on Mongo keys

Manual settlement publishes `OnBetSettlement`. A test comment (TradingRepositorySettlementTests.cs:199-203) records how the consumer, `OE.Sportsbook.Service` (outside this repo), uses it:
- The consumer finds the Mongo `Selection` by `(PrFiId, MaTeId, SeTeId, SeSp)`.
- Lines of the same market differ only by `SeSp`.
- If `SelectionSpecifier` is dropped, the filter matches nothing and the bet is never settled.

Keep these fields intact on any message that refers to a Mongo selection.

## Cache invalidation

Backoffice does not refresh Mongo. Downstream services cache some SQL data, such as sort orders and templates. When Backoffice changes that data, it publishes an `OnDeleteCacheMessage` on an `r.oe.cachesync.*` routing key. One example is `r.oe.cachesync.onsportsortorderchange` (SportsbookRepository.cs:576-588). odds-engine-2 consumes these messages (`OE.Sportsbook.Service` has `ProcessCacheInvalidationMessage`). `Backoffice.Utility` handles only the `LogActivity`, `LogActivityList` and `Export*` message types (Backoffice.Utility/Worker.cs:79-108). It neither reads nor writes Mongo.
