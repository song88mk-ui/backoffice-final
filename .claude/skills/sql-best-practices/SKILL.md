---
name: sql-best-practices
description: EF Core and SQL Server patterns for the four Backoffice DbContexts: which context holds what, lifetimes, timeouts, raw SQL, transactions and flyway-based schema changes. Use when writing or reviewing a query or a schema change.
---

# SQL Best Practices

Repository-specific EF Core and SQL Server patterns for the Backoffice solution. Apply this when writing or reviewing queries across any of the four DbContexts.

---

## Four DbContexts — which to use

| DbContext | Database | Use for |
|-----------|----------|---------|
| `BackofficeContext` | Backoffice | Clients (`ClientInformation`), bet settings (`ClientBetSetting`), config templates and margins, activity logs, client import batches |
| `TradingContext` | Trading | OddsEngine ID mappings (`SportMapping`, `SegmentMapping`, `LeagueMapping`, `FixtureMapping`, `MarketMapping`, `SelectionMapping`), plus `ActivityLog`, users, groups and permissions, price boost config, notifications, client league/market config |
| `SportsbookContext` | Sportsbook | Per-client sport/segment/league ordering and market templates only (no fixture/market/selection master data; that is in Mongo) |
| `TranslationContext` | Translation | `BackofficeStatic`, `SportsbookStatic`, `TranslationCache2` |

All four are defined in `Backoffice.Shared/Data/`. Inject only the contexts a repository actually needs — do not inject all four by default.

## DbContext lifecycle

- **Backoffice.API**: Scoped, one instance per HTTP request. `BackofficeContext` and `TranslationContext` are registered with `AddDbContextPool`, `SportsbookContext` and `TradingContext` with `AddDbContext` (`Backoffice.API/Startup.cs:91-102`).
- **Background services**: no DI-registered contexts and no `CreateScope`. The Singleton repositories build a `DbContextOptionsBuilder` from configuration and `new` the context, either once in the constructor or per call in a `using` block (`Backoffice.Utility/Repository/BackofficeRepository.cs:38-54`). Prefer the per-call `using` form; a context held on a Singleton is shared across messages.

## Query timeout

All API contexts use `Custom:DbContextTimeout`, default **180 seconds** (`Backoffice.API/Startup.cs:90-102`). Queries that routinely approach this limit need to be redesigned — do not increase the timeout.

## Required EF patterns

### AsNoTracking on every read

```csharp
// GOOD
var clients = await _backofficeContext.ClientInformation
    .AsNoTracking()
    .Where(c => c.StatusId == (int)ClientStatus.Normal)
    .ToListAsync();

// BAD — change tracker adds memory and CPU overhead for data never modified
var clients = await _backofficeContext.ClientInformation
    .Where(c => c.StatusId == (int)ClientStatus.Normal)
    .ToListAsync();
```

### Navigations and Include

Lazy loading is not configured, and the entities here have almost no navigation properties: `ConfigTemplate` has none, its margins are the separate `ConfigTemplateMargin` DbSet. No EF `.Include(` call exists in the solution. Load related rows with a second query keyed by IDs (see N+1 below), or add `.Include` before materialisation if you add a navigation.

### Always async

```csharp
// GOOD
await _context.SaveChangesAsync();
var result = await query.ToListAsync();
var item = await query.FirstOrDefaultAsync();

// BAD — blocks thread pool thread
_context.SaveChanges();
query.ToList();
query.FirstOrDefault();
```

### Raw SQL: avoid new usages, always parameterise

Five raw SQL calls exist, all stored-procedure calls with parameters: `SportsbookRepository.cs:470,987`, `ClientImportRepository.cs:376`, `BackOfficeRepository.cs:1531` (`FromSqlRaw`), `BackOfficeRepository.cs:2100`. Do not add new ones where LINQ works. If you must, pass values as `{0}` placeholders or `SqlParameter`, never by string interpolation:

```csharp
await _sportsbookContext.Database.ExecuteSqlRawAsync("EXEC DeleteClientSportSettings {0}", client.ClientId);
```

## Multi-context writes — TransactionScope

When a single API operation writes to more than one DbContext:

```csharp
using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    _backofficeContext.ClientBetSetting.Add(setting);
    await _backofficeContext.SaveChangesAsync();

    _translationContext.BackofficeStatic.Add(label);   // illustrative second context
    await _translationContext.SaveChangesAsync();

    transaction.Complete();  // Both commits or neither
}
```

Existing usages pass `TransactionScopeOption.Required`, `IsolationLevel.ReadCommitted` and `TransactionScopeAsyncFlowOption.Enabled` (e.g. `BackOfficeRepository.cs:72`, `SportsbookRepository.cs:476`); `Backoffice.Utility/Repository/BackofficeRepository.cs:56,123` does the same in a background service. Follow that form.

## Adding a new column: safe path

There are no EF migrations in this repo. Schema lives in flyway scripts in `../infrastructure/flyway/migrations/` (per environment), and every script must be idempotent (see CLAUDE.md, "SQL idempotency").

1. Add a nullable column (or one with a default) in a flyway script guarded with `IF COL_LENGTH(...) IS NULL`.
2. Add the property to the entity in `Backoffice.Shared/Data/<Context>/`, nullable so existing rows are safe.
3. Update repository queries and DTOs. `Backoffice.Shared` changes affect every service; see CLAUDE.md.

## N+1 detection pattern

Look for any query inside a `foreach` or `.Select()` with `await`:

```csharp
// BAD — N+1
var clients = await _backofficeContext.ClientInformation.AsNoTracking().ToListAsync();
foreach (var client in clients)
{
    // One DB round-trip per client
    var config = await _backofficeContext.ClientBetSetting
        .FirstOrDefaultAsync(s => s.ClientId == client.ClientId);
}

// GOOD — batch fetch by IDs
var ids = clients.Select(c => (long)c.ClientId).ToList();
var settings = await _backofficeContext.ClientBetSetting
    .AsNoTracking()
    .Where(s => ids.Contains(s.ClientId))
    .ToListAsync();
```

## Pagination for large result sets

Activity log and historical data queries must be paginated — never return unbounded result sets:

```csharp
var page = 1;
var pageSize = 50;

// ActivityLog has no ClientId; filter on Page, UserId or TimeStamp
var results = await _backofficeContext.ActivityLog
    .AsNoTracking()
    .Where(a => a.Page == pageName)
    .OrderByDescending(a => a.TimeStamp)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

## Column selection — avoid SELECT *

When building API responses that need only a subset of fields, project to an anonymous type or DTO:

```csharp
var names = await _backofficeContext.ClientInformation
    .AsNoTracking()
    .Where(c => c.StatusId == (int)ClientStatus.Normal)
    .Select(c => new { c.ClientId, c.Name })  // Only two columns over the wire
    .ToListAsync();
```
