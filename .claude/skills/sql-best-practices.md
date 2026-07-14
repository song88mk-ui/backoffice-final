# SQL Best Practices

Repository-specific EF Core and SQL Server patterns for the Backoffice solution. Apply this when writing or reviewing queries across any of the four DbContexts.

---

## Four DbContexts — which to use

| DbContext | Database | Use for |
|-----------|----------|---------|
| `BackofficeContext` | Backoffice | Clients, bet settings, config templates, activity logs |
| `TradingContext` | Trading | OddsEngine ID mappings (Sport/League/Market/Selection) |
| `SportsbookContext` | Sportsbook | Fixture/Market/Selection master data (SQL side) |
| `TranslationContext` | Translation | All entity translations by language code |

All four are defined in `Backoffice.Shared/Data/`. Inject only the contexts a repository actually needs — do not inject all four by default.

## DbContext lifecycle

- **Backoffice.API**: Scoped (one instance per HTTP request, via `AddDbContextPool`)
- **Background services**: Instantiated per message — create, use, dispose within the message handler

Background services must **not** share a DbContext instance across messages. Create a new scope per message:

```csharp
using var scope = _serviceProvider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<BackofficeContext>();
```

## Query timeout

All contexts are configured with a **180-second command timeout** in `Startup.cs`. Queries that routinely approach this limit need to be redesigned — do not increase the timeout.

## Required EF patterns

### AsNoTracking on every read

```csharp
// GOOD
var clients = await _context.Clients
    .AsNoTracking()
    .Where(c => c.Status == ClientStatus.Active)
    .ToListAsync();

// BAD — change tracker adds memory and CPU overhead for data never modified
var clients = await _context.Clients
    .Where(c => c.Status == ClientStatus.Active)
    .ToListAsync();
```

### Include before materialisation

```csharp
// GOOD — Include before ToListAsync
var templates = await _context.ConfigTemplates
    .AsNoTracking()
    .Include(t => t.Margins)
    .ToListAsync();

// BAD — lazy loading not configured; navigation property will be null
var templates = await _context.ConfigTemplates.AsNoTracking().ToListAsync();
var margins = templates[0].Margins;  // null
```

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

### No raw SQL

```csharp
// BAD
var result = await _context.Database
    .ExecuteSqlRawAsync($"SELECT * FROM Clients WHERE Id = {id}");  // SQL injection risk

// GOOD
var result = await _context.Clients
    .AsNoTracking()
    .FirstOrDefaultAsync(c => c.Id == id);
```

## Multi-context writes — TransactionScope

When a single API operation writes to more than one DbContext:

```csharp
using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    _backofficeContext.ClientBetSettings.Add(setting);
    await _backofficeContext.SaveChangesAsync();

    _translationContext.ClientTranslations.Add(translation);
    await _translationContext.SaveChangesAsync();

    transaction.Complete();  // Both commits or neither
}
```

**Restriction**: Only use `TransactionScope` in `Backoffice.API`. Background services (`Config.Service`, `Utility`, `WebSocket.Service`) are fire-and-forget and must not wrap operations in transactions.

## Adding a new column — safe migration path

1. **Add nullable column (or with default)**:
```csharp
// Entity
public string Notes { get; set; }  // nullable — safe for existing rows

// Migration
migrationBuilder.AddColumn<string>(
    name: "Notes",
    table: "ClientInformation",
    nullable: true);
```

2. Create migration: `dotnet ef migrations add AddClientNotes --project Backoffice.API`
3. Review the generated migration — verify only the intended table is touched.
4. Apply: `dotnet ef database update --project Backoffice.API`
5. Update repository queries and DTOs.

## N+1 detection pattern

Look for any query inside a `foreach` or `.Select()` with `await`:

```csharp
// BAD — N+1
var clients = await _context.Clients.AsNoTracking().ToListAsync();
foreach (var client in clients)
{
    // One DB round-trip per client
    var config = await _context.ClientBetSettings
        .FirstOrDefaultAsync(s => s.ClientId == client.Id);
}

// GOOD — single query with Include
var clients = await _context.Clients
    .AsNoTracking()
    .Include(c => c.BetSettings)
    .ToListAsync();

// GOOD — batch fetch by IDs
var ids = clients.Select(c => c.Id).ToList();
var settings = await _context.ClientBetSettings
    .AsNoTracking()
    .Where(s => ids.Contains(s.ClientId))
    .ToListAsync();
```

## Pagination for large result sets

Activity log and historical data queries must be paginated — never return unbounded result sets:

```csharp
var page = 1;
var pageSize = 50;

var results = await _context.ActivityLogs
    .AsNoTracking()
    .Where(a => a.ClientId == clientId)
    .OrderByDescending(a => a.Timestamp)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

## Column selection — avoid SELECT *

When building API responses that need only a subset of fields, project to an anonymous type or DTO:

```csharp
var names = await _context.Clients
    .AsNoTracking()
    .Where(c => c.Status == ClientStatus.Active)
    .Select(c => new { c.ClientId, c.ClientName })  // Only two columns over the wire
    .ToListAsync();
```
