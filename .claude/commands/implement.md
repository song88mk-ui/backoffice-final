# /implement

Implement a new feature or endpoint in the Backoffice solution following CLAUDE.md conventions end-to-end.

## Usage

```
/implement $ARGUMENTS
```

`$ARGUMENTS` describes the feature in plain language, e.g.:
- `GET /api/client/{clientId}/margins endpoint`
- `RabbitMQ consumer for OnResetClientConfigMessage`
- `cache invalidation for client league config`

## Implementation checklist

Work through these steps in order. Complete each before moving to the next.

### Step 1 — Shared models (Backoffice.Shared)
- Add request DTO to `Backoffice.Shared/Models/Requests/` if a new request shape is needed.
- Add response DTO to `Backoffice.Shared/Models/Response/` if a new response shape is needed.
- Add message class to `Backoffice.Shared/Models/OutgoingEngineMessages/` if async messaging is involved.
- Naming: `XxxRequest`, `XxxResponse`, `OnXxxMessage`.

### Step 2 — Repository interface (Backoffice.API)
- Add method signature to `IXxxRepository.cs`.
- Return type: `Task<BaseResponse<T>>` for data, `Task<BaseResponse<bool>>` for mutations.
- Name: `VerbNounAsync` (e.g. `GetClientMarginAsync`, `AddClientMarginAsync`).

### Step 3 — Repository implementation (Backoffice.API)
Follow this exact template:

```csharp
public async Task<BaseResponse<T>> GetXxxAsync(int id)
{
    try
    {
        var result = await _context.Xxx
            .AsNoTracking()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();

        return new BaseResponse<T> { Result = result };
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to get Xxx {Id}: {Exception}", id, ex);
        return new BaseResponse<T> { StatusCode = StatusCode.Error };
    }
}
```

Rules:
- Use `AsNoTracking()` on every read query.
- Wrap in try/catch; log with named parameters; return `StatusCode.Error` on failure.
- For multi-context writes use `TransactionScope(TransactionScopeAsyncFlowOption.Enabled)`.
- Never use `new` to instantiate services/repositories inside methods.

### Step 4 — Controller (Backoffice.API/Controllers/)
```csharp
[HttpGet("{id}")]
[Authorize]
public async Task<IActionResult> GetXxx(int id)
{
    var response = await _repository.GetXxxAsync(id);
    return Ok(response);
}
```

Rules:
- All endpoints need `[Authorize]` (JWT) or `[ApiKey]` — choose based on caller type.
- Return `Ok(response)` wrapping `BaseResponse<T>`.
- Keep controller thin; all logic lives in the repository.

### Step 5 — Cache (if applicable)
- Read through `IMemCache` with lazy-load pattern:
```csharp
var cached = _memCache.GetCache<T>("entity.operation.id",
    () => _context.Xxx.AsNoTracking().FirstOrDefault(x => x.Id == id),
    expiration: 1);
```
- Invalidate on mutation:
```csharp
_memCache.DeleteCache("entity.operation.id");
await _rabbitMqRepository.Publish(new OnDeleteCacheMessage
{
    CacheKey = "entity.operation.id",
    ClientId = clientId
}, region: "HK");
```

### Step 6 — Message publishing (if async)
```csharp
await _rabbitMqRepository.Publish(new OnXxxMessage
{
    // fields
    CorrelationId = Guid.NewGuid().ToString().ToUpper()
}, region: "HK");
```

### Step 7 — Message consuming (if background service)
In the appropriate `Worker.cs` switch block, add:
```csharp
case "XxxMessageType":
    var data = JsonSerializer.Deserialize<XxxModel>(payload);
    await _repository.HandleXxxAsync(data);
    break;
```

### Step 8 — DI registration (Startup.cs)
Register any new repository/service in the correct `Startup.cs`:
```csharp
services.AddScoped<IXxxRepository, XxxRepository>();
```

## Output format
For each step that applies, show the **complete file diff** (or full new file if it is new). Never produce partial snippets for files that must compile. After all diffs, list any follow-up items (migrations, config changes, downstream service updates).
