# /mongodb

Work with MongoDB collections in the Backoffice solution — query, index, or fix operations against the Sportsbook database.

## Usage

```
/mongodb $ARGUMENTS
```

`$ARGUMENTS` examples:
- `find all fixtures for leagueId 42`
- `update fixture status for fixtureId 100`
- `add index on Fixtures.LeagueId`
- `diagnose slow query in SportsbookRepository.GetFixtureAsync`
- `review SportsbookRepository.cs`

## MongoDB context in this solution

- **Database:** `Sportsbook`
- **Collections:** `Fixtures`, `Markets`, `Selections`, `Sports`, `Leagues`, `Segments`
- **Role:** Cache layer — SQL Server is source of truth; MongoDB is the fast-read replica
- **Invalidation:** Cache invalidation is triggered via RabbitMQ `OnDeleteCacheMessage`

## Connection pattern

```csharp
private static IMongoDatabase _db;  // Lazy static — known thread-safety risk

public SportsbookRepository(IConfiguration configuration)
{
    if (_db == null)
    {
        var connectionString = configuration["MongoDB:ConnectionString"];
        var client = new MongoClient(connectionString);
        _db = client.GetDatabase("Sportsbook");
    }
}
```

**Warning:** The static `_db` initialization is not thread-safe (known issue). Do not introduce additional static state.

## Read pattern (required)

All reads must specify `ReadPreference.PrimaryPreferred`:

```csharp
public async Task<BaseResponse<Fixture>> GetFixtureAsync(long fixtureId)
{
    try
    {
        var collection = _db.GetCollection<Fixture>("Fixtures");
        var filter = Builders<Fixture>.Filter.Eq(f => f.FixtureId, fixtureId);
        var options = new FindOptions<Fixture> { ReadPreference = ReadPreference.PrimaryPreferred };

        var cursor = await collection.FindAsync(filter, options);
        var result = await cursor.FirstOrDefaultAsync();

        return new BaseResponse<Fixture> { Result = result };
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to get Fixture {FixtureId}: {Exception}", fixtureId, ex);
        return new BaseResponse<Fixture> { StatusCode = StatusCode.Error };
    }
}
```

## Write pattern

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

## Query rules

1. **Never query without a filter** — full collection scans are expensive on production data.
2. **Always use `ReadPreference.PrimaryPreferred`** on reads.
3. **Use typed builders** — `Builders<T>.Filter`, `Builders<T>.Sort`, `Builders<T>.Update`.
4. **Project fields when possible** — use `Builders<T>.Projection` to avoid loading large documents.
5. **Wrap in try/catch** — return `BaseResponse<T>` with `StatusCode.Error` on failure.

## Index recommendations

When adding a query on a new field, also add an index:

```csharp
// Index creation (run once, in a migration/seed method):
var collection = _db.GetCollection<Fixture>("Fixtures");
var indexKeys = Builders<Fixture>.IndexKeys.Ascending(f => f.LeagueId);
var indexOptions = new CreateIndexOptions { Name = "idx_fixtures_leagueid" };
await collection.Indexes.CreateOneAsync(new CreateIndexModel<Fixture>(indexKeys, indexOptions));
```

Common indexes already expected:
- `Fixtures.FixtureId` (primary lookup)
- `Fixtures.LeagueId` (filter by league)
- `Markets.FixtureId` (join pattern)
- `Selections.MarketId` (join pattern)

## Cache invalidation for MongoDB data

When MongoDB data is updated, broadcast invalidation:

```csharp
_memCache.DeleteCache($"fixture.{fixtureId}");
await _rabbitMqRepository.Publish(new OnDeleteCacheMessage
{
    CacheKey = $"fixture.{fixtureId}",
    ClientId = 0  // 0 = global invalidation
}, region: "HK");
```

## Output format

For query or implementation tasks: show the complete repository method including try/catch, filter, and `ReadPreference`.
For index tasks: show the `CreateIndexModel` call and where to run it.
For diagnosis: identify the slow pattern and show the corrected version with explanation.
