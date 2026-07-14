# MongoDB Best Practices

Repository-specific patterns for MongoDB usage in the Backoffice solution. The MongoDB database (`Sportsbook`) is a cache layer — SQL Server is source of truth. Apply this when working with `SportsbookRepository` or any code touching the `Sportsbook` collections.

---

## Connection pattern and known risk

The repository uses a lazy static singleton — this is the **existing pattern**, not a recommendation:

```csharp
private static IMongoDatabase _db;  // Shared across all instances of SportsbookRepository

public SportsbookRepository(IConfiguration configuration)
{
    if (_db == null)  // Not thread-safe — race condition possible at startup
    {
        var client = new MongoClient(configuration["MongoDB:ConnectionString"]);
        _db = client.GetDatabase("Sportsbook");
    }
}
```

**Do not add more static state.** If you must work around the race, use `Interlocked.CompareExchange` or a `Lazy<IMongoDatabase>` — but do not change the existing initialization without a coordinated deployment.

## Collections and document shapes

| Collection | Key fields | Primary lookup |
|------------|-----------|----------------|
| `Fixtures` | `FixtureId (long)`, `LeagueId`, `SportId`, `Status` | `FixtureId` |
| `Markets` | `MarketId (long)`, `FixtureId`, `MarketTypeId` | `FixtureId` → `MarketId` |
| `Selections` | `SelectionId (long)`, `MarketId`, `OddsValue (long)` | `MarketId` → `SelectionId` |
| `Sports` | `SportId (int)`, `Name` | `SportId` |
| `Leagues` | `LeagueId (int)`, `SportId`, `Name` | `LeagueId` |
| `Segments` | `SegmentId (int)`, `ParentSegmentId` | `SegmentId` |

## Mandatory read pattern

Every read must specify `ReadPreference.PrimaryPreferred`:

```csharp
private async Task<T> FindOneAsync<T>(string collectionName, FilterDefinition<T> filter)
    where T : class
{
    var collection = _db.GetCollection<T>(collectionName);
    var options = new FindOptions<T> { ReadPreference = ReadPreference.PrimaryPreferred };
    var cursor = await collection.FindAsync(filter, options);
    return await cursor.FirstOrDefaultAsync();
}
```

Never omit `ReadPreference` — it allows reads from replica nodes, reducing load on the primary.

## Filter construction — always use Builders

```csharp
// GOOD — typed, indexed field
var filter = Builders<Fixture>.Filter.Eq(f => f.FixtureId, fixtureId);

// GOOD — compound filter
var filter = Builders<Fixture>.Filter.And(
    Builders<Fixture>.Filter.Eq(f => f.LeagueId, leagueId),
    Builders<Fixture>.Filter.Eq(f => f.Status, FixtureStatus.Live));

// BAD — full collection scan
var filter = FilterDefinition<Fixture>.Empty;

// BAD — unindexed string field without compound index
var filter = Builders<Fixture>.Filter.Eq(f => f.SomeFreeTextField, value);
```

## Upsert pattern (cache refresh)

When refreshing cache from SQL source of truth:

```csharp
public async Task<BaseResponse<bool>> UpsertFixtureAsync(Fixture fixture)
{
    try
    {
        var collection = _db.GetCollection<Fixture>("Fixtures");
        var filter = Builders<Fixture>.Filter.Eq(f => f.FixtureId, fixture.FixtureId);
        var options = new ReplaceOptions { IsUpsert = true };

        await collection.ReplaceOneAsync(filter, fixture, options);
        return new BaseResponse<bool> { Result = true };
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to upsert Fixture {FixtureId}: {Exception}", fixture.FixtureId, ex);
        return new BaseResponse<bool> { StatusCode = StatusCode.Error };
    }
}
```

## Bulk operations

When syncing multiple documents (e.g. after a fixture batch update), use bulk write — never loop with individual upserts:

```csharp
var requests = fixtures.Select(f =>
    new ReplaceOneModel<Fixture>(
        Builders<Fixture>.Filter.Eq(x => x.FixtureId, f.FixtureId),
        f)
    { IsUpsert = true }
).ToList();

await collection.BulkWriteAsync(requests);
```

## Projection — reduce document payload

When only a subset of fields is needed, always project:

```csharp
var projection = Builders<Fixture>.Projection
    .Include(f => f.FixtureId)
    .Include(f => f.Status)
    .Exclude(f => f.Id);  // Exclude Mongo _id unless needed

var options = new FindOptions<Fixture, BsonDocument>
{
    Projection = projection,
    ReadPreference = ReadPreference.PrimaryPreferred
};
```

## Index checklist before adding a new query

Before adding any `FindAsync` call on a field that isn't `FixtureId`, `MarketId`, or `SelectionId`:

1. Check if an index exists on that field in the production collection.
2. If not, add an index creation call (run once, in a startup/migration method):

```csharp
var indexKeys = Builders<Fixture>.IndexKeys
    .Ascending(f => f.LeagueId)
    .Ascending(f => f.Status);  // Compound index for common query pattern

await collection.Indexes.CreateOneAsync(
    new CreateIndexModel<Fixture>(
        indexKeys,
        new CreateIndexOptions { Name = "idx_fixtures_league_status", Background = true }));
```

`Background: true` — prevents blocking reads during index build on large collections.

## Cache invalidation flow for MongoDB data

MongoDB is a cache — when SQL source data changes, MongoDB must be updated and in-memory cache cleared:

```
SQL mutation (BackofficeContext / SportsbookContext)
    ↓
Publish OnDeleteCacheMessage via RabbitMQ
    ↓
Backoffice.Utility receives message
    ↓
Utility refreshes MongoDB document from SQL + clears IMemCache key
```

Do not write directly from the API to MongoDB — always go through the message flow.
