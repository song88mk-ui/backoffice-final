# Database Flow & Data Access Patterns

**Document Status:** Analysis Complete (No Code Changes Made)  
**Date:** June 18, 2026

## Database Architecture

### Multi-Database Strategy

```
Four Separate SQL Server Databases
├─ Backoffice Database
│  └─ BackofficeContext DbSet
│
├─ Trading Database
│  └─ TradingContext DbSet
│
├─ Sportsbook Database
│  └─ SportsbookContext DbSet (SQL)
│  └─ MongoDB (parallel cache)
│
└─ Translation Database
   └─ TranslationContext DbSet
```

### Why Separate Databases?

1. **Scalability** - Each can be scaled independently
2. **Isolation** - Failure in one doesn't affect others
3. **Performance** - Smaller databases = faster queries
4. **Multi-tenancy** - Potential for region-specific databases
5. **Access Control** - Different permission sets per database

---

## Database Contexts

### 1. BackofficeContext

**Purpose:** Core operational data for backoffice operations

**Tables:**
```
ActivityLog (ActivityLog2)
├─ Id (PK)
├─ Page, Payload, Activity
├─ From, To (user info)
├─ UserId, UserName
└─ TimeStamp

ClientInformation
├─ ClientId (PK)
├─ Client metadata

ClientBetSetting
├─ ClientBetSettingId (PK)
├─ Bet-specific settings

ConfigTemplate
├─ Id (PK)
├─ Template configurations

ConfigTemplateMargin
├─ (ConfigTemplateId, SportId, FixtureTypeId, FixtureStatusId, MarketTemplateId) (PK)
├─ Configuration margins

ConfigTemplateAssigned
├─ Id (PK)
├─ Template assignments

ConfigTemplateAssignedMapping
├─ (ConfigTemplateAssignedId, ConfigTemplateId, FixtureStatusId) (PK)
├─ Assignment mappings

ClientImportBatch                        (CSV client import/update, BO-112)
├─ BatchId (PK, client-generated GUID so the UI can poll progress)
├─ Environment, OperationType (Create | Update)
├─ Status (Running | Completed | PartiallySuccessful | Failed)
├─ StartedByUserId, StartedByUserName, StartedAt, CompletedAt
├─ TotalRows, SucceededRows, FailedRows
└─ Filtered unique index on Environment WHERE Status = 'Running' (one batch at a time per environment)

ClientImportBatchRow
├─ Id (PK), BatchId (FK → ClientImportBatch)
├─ RowNumber, ClientCode, ClientName
├─ Status (Pending | Succeeded | Failed | Skipped)
└─ StepsCompleted, ErrorMessage (≤1000 chars), ResultClientId (Platform client GUID)
```

**Client identifiers.** A client has three, and only one is "Client ID":

| Name in UI / CSV | Column | Type | Notes |
|---|---|---|---|
| **Client ID** | `Platform.dbo.Client.ClientId` | `uniqueidentifier` | Assigned by Platform on create, never changes. The id every other system uses (Betting, Sportsbook, Trading, Operator tables, `clientId=` in sportsbook URLs). |
| Client Code / Brand | `ClientInformation.Brand`, `Platform.dbo.Client.ClientCode` | text | What admins type, e.g. `NWS`. |
| (internal) | `ClientInformation.ClientId` | `int` identity | Backoffice primary key; Platform stores it as `Client.ClientIdRef`, which is the real Backoffice↔Platform link. |

The CSV update tool identifies clients by Client ID, resolved via `ClientIdRef` (falling back to Client Code for older Platform rows without it). The Client Information modal shows it read-only.

Field-level change history for CSV updates is not a separate table — it is written to
`ActivityLog2` with `Page = "Clients - Import"`, `Activity = "Update"`,
`Payload = "{batchId}|{clientCode}|{fieldName}"`, and the old/new values in `From`/`To`.
Batch summaries use the same Page with `Payload = "{batchId}"` (no pipe).

**Key Characteristics:**
- Dense primary keys (composite keys for margins)
- Activity log with denormalized data
- Template-based configuration system

### 2. TradingContext

**Purpose:** Trading and provider mappings

**Tables:** (Inferred from repository usage)
```
Sport Mappings
├─ SportId, ExternalSportId

League Mappings
├─ LeagueId, ExternalLeagueId

Market Mappings
├─ MarketId, ExternalMarketId

Selection Mappings
├─ SelectionId, ExternalSelectionId

Provider Mappings
├─ ProviderMappingId
└─ 3rd-party provider references
```

**Key Characteristics:**
- Lookup/mapping tables primarily
- Used for multi-provider reconciliation
- Rarely updated, heavily read

### 3. SportsbookContext

**Purpose:** Sports hierarchy and fixture data (from odds engine)

**Tables (SQL Server):**
```
Fixtures
├─ FixtureId (PK)
├─ SportId, LeagueId
├─ Status, Odds
└─ Metadata

Markets
├─ MarketId (PK)
├─ FixtureId (FK)
├─ Odds, Status
└─ Selection data

Selections
├─ SelectionId (PK)
├─ MarketId (FK)
└─ Odds

Sports
├─ SportId (PK)
└─ Name, Code

Leagues
├─ LeagueId (PK)
├─ SportId (FK)
└─ Name

Segments
├─ SegmentId (PK)
└─ Hierarchical grouping
```

**Parallel MongoDB Storage:**
```
MongoDB Database: "Sportsbook"

Collections (with read preference PrimaryPreferred):
├─ SportLookup
├─ SegmentLookup
├─ LeagueLookup
└─ MarketLookup
```

**Key Characteristics:**
- Heavily queried (real-time updates)
- Dual storage for performance
- Read preference: Primary with fallback to secondary

### 4. TranslationContext

**Purpose:** Multi-language translation data

**Tables (Inferred):**
```
TranslationKeys
├─ KeyId (PK)
├─ LanguageCode
└─ TranslationValue

BackofficeStatic
├─ LanguageCode
├─ Content
└─ Metadata
```

**Key Characteristics:**
- Read-heavy (translations rarely change)
- Cached aggressively
- Multi-language support

---

## Entity Framework Core Configuration

### DbContext Pooling Strategy

**Pooled Contexts:**
```csharp
services.AddDbContextPool<BackofficeContext>(options => 
    options.UseSqlServer(connectionString))

services.AddDbContextPool<TranslationContext>(options => 
    options.UseSqlServer(connectionString))
```

**Advantages:**
- ✓ Reduces instance creation overhead
- ✓ Improves performance for scoped contexts
- ✓ Better for high-concurrency scenarios

**Non-Pooled Contexts:**
```csharp
services.AddDbContext<SportsbookContext>(options => 
    options.UseSqlServer(connectionString))

services.AddDbContext<TradingContext>(options => 
    options.UseSqlServer(connectionString))
```

**Rationale (Unclear):**
- Possibly lower query load on these databases
- Or legacy code (should be pooled too)

### Configuration Timeout

```csharp
var dbContextTimeout = Configuration.GetValue<int>(
    "Custom:DbContextTimeout", 180);  // 3 minutes default

options.CommandTimeout(dbContextTimeout)
```

**Impact:**
- All long-running queries must complete in 180 seconds
- Bulk operations may timeout
- ETL processes need optimization

---

## Data Access Patterns

### Pattern 1: Simple Query with Caching

```csharp
public async Task<BaseResponse<ClientList>> GetClients()
{
    try
    {
        var cacheKey = "all_clients";
        var cached = _memCache.GetCache<ClientList>(cacheKey);
        if (cached != null) return new BaseResponse { Result = cached };
        
        var result = await _context.ClientInformation
            .AsNoTracking()
            .ToListAsync();
        
        _memCache.AddOrUpdateCache(cacheKey, result, cacheTimeout: 1);
        return new BaseResponse { Result = result };
    }
    catch (Exception ex)
    {
        return new BaseResponse { StatusCode = StatusCode.Error };
    }
}
```

### Pattern 2: Complex Query with Transaction

```csharp
public async Task<BaseResponse<bool>> AddActivityLog(ActivityLogModel request)
{
    using (var scope = new TransactionScope(
        TransactionScopeOption.Required,
        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
        TransactionScopeAsyncFlowOption.Enabled))
    {
        try
        {
            var entity = new ActivityLog { ... };
            _context.ActivityLog.Add(entity);
            await _context.SaveChangesAsync();
            
            scope.Complete();
            return new BaseResponse<bool> { Result = true };
        }
        catch
        {
            scope.Dispose();  // Automatic rollback
            return new BaseResponse<bool> { StatusCode = StatusCode.Error };
        }
    }
}
```

### Pattern 3: Multi-Database Query

```csharp
public async Task<Result> ProcessOperation()
{
    // Query from Backoffice DB
    var backofficeData = await _backofficeContext.ConfigTemplate
        .Where(c => c.IsActive)
        .ToListAsync();
    
    // Query from Trading DB
    var tradingData = await _tradingContext.SportMappings
        .Where(s => s.SportId == id)
        .ToListAsync();
    
    // Query from Sportsbook DB
    var sportsbookData = await _sportsbookContext.Fixtures
        .Where(f => f.Status == "Active")
        .ToListAsync();
    
    // Merge in memory (N+1 potential)
    return Merge(backofficeData, tradingData, sportsbookData);
}
```

### Pattern 4: MongoDB Query (Static Client)

```csharp
private static IMongoCollection<SportLookup> _sportLookupCollection;

public SportsbookRepository(...)
{
    if (_db == null)
    {
        var client = new MongoClient(
            configuration["Custom:MongoDB-Sportsbook"]);
        _db = client.GetDatabase("Sportsbook");
        
        _sportLookupCollection = _db.GetCollection<SportLookup>(
            "SportLookup")
            .WithReadPreference(
                new ReadPreference(ReadPreferenceMode.PrimaryPreferred));
    }
}
```

**Issues with Static Pattern:**
- ⚠️ Thread-safety not explicitly managed
- ⚠️ Singleton pattern tied to class
- ⚠️ Cannot be mocked/tested easily

---

## Query Patterns

### AsNoTracking Usage
```csharp
// Good: Read-only queries
var clients = await _context.Clients
    .AsNoTracking()
    .ToListAsync();

// Necessary: Prevents change tracking overhead
// But: Updates handled in different context
```

### LINQ vs SQL
```csharp
// Current approach: LINQ to Entities
var result = _context.Clients
    .Where(c => c.IsActive)
    .OrderBy(c => c.Name)
    .ToListAsync();

// Alternative: Raw SQL (not visible in codebase)
// Could be faster for complex queries
```

### Lazy Loading
```csharp
// Potential N+1 issue
var templates = _context.ConfigTemplate
    .Include(t => t.Margins)  // Explicit loading
    .ToListAsync();

// Not using lazy loading (disabled in most EF configs)
```

---

## Connection Management

### Connection String Format
```
Data Source=server,1433;
Database=database_name;
User Id=admin;
Password=password;
TrustServerCertificate=True;
```

### Connection Pooling
```csharp
// SQL Server connection pooling is automatic
// Default pool size: 100 connections
// Timeout: 15 minutes of inactivity

// EF Core DbContextPool adds another layer
// of context instance pooling
```

### Multi-Region Connections
```
appsettings.json:
├─ HK Region: stg-song88-mssql-server.cz8wmg0s28pd.ap-south-1.rds.amazonaws.com
├─ Production: prd-song88-mssql-server.cdqkiuumgexn.ap-east-1.rds.amazonaws.com
└─ (SA region: not visible in config)
```

---

## Transaction Isolation

### Current Strategy: ReadCommitted

```csharp
new TransactionOptions 
{ 
    IsolationLevel = IsolationLevel.ReadCommitted 
}
```

**Characteristics:**
- ✓ Default isolation level
- ✓ Prevents dirty reads
- ✓ Allows non-repeatable reads (acceptable for most operations)
- ⚠️ May cause ghost reads

**When Used:**
- Activity log insertion
- Configuration template updates
- Client margin calculations

### Alternative Levels Not Used
- **Serializable** - Highest consistency, lowest concurrency
- **Snapshot** - Good for read-heavy workloads
- **RepeatableRead** - Better than ReadCommitted for some scenarios

---

## MongoDB Integration

### Connection Pattern

```csharp
// Single connection to MongoDB
var client = new MongoClient(
    "mongodb://10.49.100.55:27017/sportsbook?retryWrites=true&w=0");
var database = client.GetDatabase("Sportsbook");

// Collections accessed via static fields
var sportLookup = database.GetCollection<SportLookup>("SportLookup");
```

### Write Concern: w=0 (Fire & Forget)

```csharp
// Configuration: retryWrites=true&w=0
// w=0: Write not acknowledged
```

**Implications:**
- ✓ Fastest writes (no wait for acknowledgment)
- ✗ No error detection
- ✗ Data loss possible
- ✗ Replication not guaranteed

**Use Case:** Cache/replica data acceptable to lose

### Read Preference: PrimaryPreferred

```csharp
.WithReadPreference(
    new ReadPreference(ReadPreferenceMode.PrimaryPreferred))
```

**Behavior:**
- Try to read from primary
- Fallback to secondary if primary unavailable

---

## Performance Considerations

### Query Performance

| Scenario | Impact | Mitigation |
|----------|--------|-----------|
| Large result sets | Memory, network | Paging, filtering |
| N+1 queries | Database load | Includes, projections |
| Missing indexes | Slow queries | Database monitoring |
| Long transactions | Lock contention | Split into smaller units |
| Cross-database joins | N+A queries | In-memory merge |

### Caching Impact

```
Cache hit:           ~1ms
Cache miss + query:  ~100-500ms
Timeout (3 hrs):     Periodic refresh
```

### Timeout Risk

```
180-second timeout applies to:
├─ Bulk configuration updates
├─ Settlement history exports
├─ Client migration operations
└─ Report generation

Risk: Operations exceed timeout and fail
Solution: Async job queue, pagination
```

---

## Data Consistency Patterns

### Optimistic Concurrency
```csharp
// Not currently used
// Would need RowVersion column
```

### Pessimistic Concurrency (Transactions)
```csharp
// Current approach
// Locking during transaction
```

### Event Sourcing
```csharp
// Not implemented
// Activity log is closest
```

---

## Migration & Schema Management

### Current State
- No visible migration files in codebase
- Likely managed via separate migration project or manual SQL
- Schema assumptions hardcoded in DbContexts

### OnModelCreating Configuration

```csharp
modelBuilder.Entity<ClientInformation>()
    .HasKey(k => new { k.ClientId });

modelBuilder.Entity<ConfigTemplateMargin>()
    .HasKey(k => new { 
        k.ConfigTemplateId, 
        k.SportId, 
        k.FixtureTypeId, 
        k.FixtureStatusId, 
        k.MarketTemplateId });
```

### Missing Configurations
- No foreign key relationships defined
- No default values
- No column constraints
- No indexes

---

## Stored Procedures

### ActivityLog Stored Procedure (Implied)

```csharp
public DbSet<ActivityLog> ReadActivityLog { get; set; }
```

**Purpose:** Likely for efficient activity log reading

**Not explicitly implemented in visible code**

---

## Summary Table

| Aspect | Implementation | Status |
|--------|-----------------|--------|
| Multi-database | 4 separate SQLServers | Production |
| Context pooling | Partial (2 of 4) | Could improve |
| Transactions | TransactionScope + ReadCommitted | Production |
| Caching | In-process MemoryCache | Production |
| MongoDB | Static connection | Production |
| Indexes | Not visible | Unknown |
| Migrations | Not visible | Unknown |
| Query optimization | Basic LINQ | Needs review |
| Connection pooling | SQL Server default | Production |
| Timeout | 180 seconds hardcoded | May be risky |

