# /refactor

Safely refactor a specific file or method in the Backoffice solution. Scope is strictly limited — no drive-by changes outside the target.

## Usage

```
/refactor $ARGUMENTS
```

`$ARGUMENTS`: the file path, class name, or method to refactor, optionally followed by the goal, e.g.:
- `Backoffice.API/Repositories/BackofficeRepository.cs GetClientsAsync — extract caching logic`
- `ClientController — split into smaller action methods`

## Ground rules (read before touching any code)

1. **Minimal scope.** Only change what is named in `$ARGUMENTS`. Do not reorganize adjacent methods, rename unrelated variables, or move files.
2. **No behavior changes.** The refactored code must produce identical outputs for identical inputs.
3. **Preserve API contracts.** Method signatures, return types, and HTTP routes must stay the same.
4. **Keep backward compatibility.** Do not remove fields from `BaseResponse<T>` subclasses or message DTOs.
5. **Preserve error handling.** Every `try/catch` block must survive the refactor.

## Standard refactoring patterns

### Extract private helper method
Move repeated logic inside a repository to a `#region Private Methods` helper:

```csharp
#region Private Methods
private async Task<XxxModel> FetchXxxInternalAsync(int id)
{
    return await _context.Xxx
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == id);
}
#endregion
```

### Reduce nesting in try/catch
Use early-return inside `try` to avoid deep nesting:

```csharp
try
{
    var entity = await _context.Xxx.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    if (entity == null)
        return new BaseResponse<T> { StatusCode = StatusCode.NotFound };

    return new BaseResponse<T> { Result = entity };
}
catch (Exception ex)
{
    _logger.LogError("Failed to get Xxx {Id}: {Exception}", id, ex);
    return new BaseResponse<T> { StatusCode = StatusCode.Error };
}
```

### Consolidate duplicate cache invalidation
Replace repeated `_memCache.DeleteCache` + `_rabbitMqRepository.Publish` pairs with a shared private method:

```csharp
private async Task InvalidateCacheAsync(string cacheKey, int clientId)
{
    _memCache.DeleteCache(cacheKey);
    await _rabbitMqRepository.Publish(new OnDeleteCacheMessage
    {
        CacheKey = cacheKey,
        ClientId = clientId
    }, region: "HK");
}
```

### Split oversized repository (2000+ lines)
Only do this when explicitly asked. Steps:
1. Identify logical domains within the file (e.g. client config vs. market config).
2. Create `IXxxPartRepository` and `XxxPartRepository` for each domain.
3. Register in `Startup.cs`.
4. Update constructor of existing repository to inject new sub-repositories.
5. Redirect old public methods to the new sub-repositories.
6. Do NOT change method signatures.

## Output format

Produce a **before/after diff** for every changed block. State the refactoring rationale in one sentence per change. If a follow-up step is needed (e.g. DI registration), list it explicitly.
