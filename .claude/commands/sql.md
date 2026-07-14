# /sql

Work with Entity Framework Core and SQL Server in the Backoffice solution — query design, migrations, or DbContext changes.

## Usage

```
/sql $ARGUMENTS
```

`$ARGUMENTS` examples:
- `add ClientNotes column to ClientInformation table`
- `query all active clients with their bet settings`
- `add migration for new ConfigTemplateMargin column`
- `diagnose slow query in BackofficeRepository.GetActivityLogAsync`
- `review TradingRepository.cs for EF anti-patterns`

## Four-database architecture

| DbContext | Database | Key entities |
|-----------|----------|-------------|
| `BackofficeContext` | Backoffice | `ClientInformation`, `ClientBetSetting`, `ConfigTemplate`, `ConfigTemplateMargin` |
| `TradingContext` | Trading | Sport/League/Market/Selection mappings |
| `SportsbookContext` | Sportsbook | Fixture/Market/Selection data |
| `TranslationContext` | Translation | `FixtureTranslation`, `LeagueTranslation`, `SportTranslation`, etc. |

## EF Core conventions

### Read queries — required patterns
```csharp
var clients = await _backofficeContext.Clients
    .AsNoTracking()                          // Required on all reads
    .Where(c => c.Status == ClientStatus.Active)
    .Include(c => c.BetSettings)             // Load navigation properties explicitly
    .ToListAsync();                          // Always async
```

### Write operations
```csharp
_backofficeContext.Clients.Add(entity);
await _backofficeContext.SaveChangesAsync();
```

### Multi-context transactions
When a single operation writes to more than one DbContext, use `TransactionScope`:

```csharp
using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    _backofficeContext.Xxx.Add(entityA);
    await _backofficeContext.SaveChangesAsync();

    _translationContext.XxxTranslations.Add(entityB);
    await _translationContext.SaveChangesAsync();

    transaction.Complete();
}
```

**Only use `TransactionScope` in Backoffice.API.** Background services (Config.Service, Utility, WebSocket.Service) must not use it.

### No raw SQL
Use EF Core LINQ queries exclusively. Raw SQL strings are forbidden:

```csharp
// BAD
var result = await _context.Database.ExecuteSqlRawAsync("SELECT * FROM Clients WHERE Id = {0}", id);

// GOOD
var result = await _context.Clients.AsNoTracking()
    .FirstOrDefaultAsync(c => c.Id == id);
```

## Adding a new column (migration)

### Step 1 — Add property to entity
```csharp
// In Backoffice.Shared/Data/BackofficeContext.cs or entity class
public string Notes { get; set; }
```

### Step 2 — Add migration
```powershell
dotnet ef migrations add AddClientNotesColumn --project Backoffice.API
```

### Step 3 — Review generated migration
Check that the migration only touches the intended table and column. Look for accidental drops.

### Step 4 — Apply migration
```powershell
dotnet ef database update --project Backoffice.API
```

### Step 5 — Update repository
Add the new column to relevant queries and response DTOs.

### Safe migration rules
- New columns must be **nullable** or have a **default value** to avoid breaking existing rows.
- Never drop a column in the same migration as adding a replacement — use a two-step approach.
- Never change a column's data type without a migration period.

## Query timeout

All database operations have a **180-second timeout** configured in Startup.cs. Do not write queries that approach this limit in normal operation. Flag any query that risks it.

## Common anti-patterns to fix

| Anti-pattern | Fix |
|---|---|
| Missing `AsNoTracking()` | Add to all read queries |
| `.ToList()` (sync) | Replace with `.ToListAsync()` |
| `Include()` after `ToList()` (lazy) | Move `Include()` before `ToListAsync()` |
| N+1 (query in loop) | Batch with `.Where(x => ids.Contains(x.Id))` |
| Raw SQL string | Rewrite as LINQ expression |
| `new DbContext()` inside method | Inject via constructor |

## Output format

For schema changes: produce the entity diff, migration command, and any repository changes.
For query fixes: produce before/after diff with one-line explanation per change.
For diagnosis: identify each anti-pattern with file path, line number, and corrected code.
