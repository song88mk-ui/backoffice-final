# /bug

Diagnose and fix a bug in the Backoffice solution. Follow the bug-fixing workflow from CLAUDE.md.

## Usage

```
/bug $ARGUMENTS
```

`$ARGUMENTS`: plain description of the bug, error message, or file/method suspected, e.g.:
- `NullReferenceException in BackofficeRepository.GetClientAsync`
- `Cache not invalidated after UpdateClientMargin`
- `OnDeleteCacheMessage not consumed by Backoffice.Utility`
- `ClientController POST returns 500 with no log entry`

## Diagnosis process

### Step 1 — Reproduce
- Identify which project and layer exhibits the bug (API, background service, WebSocket).
- Find the exact code path: controller → repository → database/cache/broker.
- State the observed vs. expected behavior.

### Step 2 — Root cause
Examine the code and name the root cause precisely. Common root causes in this codebase:

| Category | Typical causes |
|----------|----------------|
| Null reference | Missing `.Include()` on navigation property; uninitialized `BaseResponse.Result` |
| Cache stale | `OnDeleteCacheMessage` not published after mutation; consumer not handling message type |
| Message not consumed | New message type missing from switch block in `Worker.cs`; wrong routing key |
| DB error silenced | Exception caught but `StatusCode.Error` not checked by caller |
| Race condition | Static MongoDB `_db` field initialized without locking |
| Auth failure | Missing `[Authorize]` or `[ApiKey]` attribute on endpoint |
| Serialization | `Newtonsoft.Json` vs `System.Text.Json` mismatch between producer and consumer |

### Step 3 — Affected services
List every project that needs a change:
- Backoffice.API
- Backoffice.Config.Service
- Backoffice.Utility
- Backoffice.WebSocket.Service
- Backoffice.Shared (if model change)

### Step 4 — Fix
Apply the **minimal** fix. Rules:
- Do not refactor unrelated code.
- Do not rename variables for aesthetics.
- Preserve method signatures.
- Keep backward compatibility.
- Add or correct logging where it was missing.

Fix template for repository-level bugs:
```csharp
// Before
var result = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
return new BaseResponse<ClientInformationModel> { Result = result };

// After — added null check + NotFound status
var result = await _context.Clients
    .AsNoTracking()
    .FirstOrDefaultAsync(c => c.Id == id);

if (result == null)
    return new BaseResponse<ClientInformationModel> { StatusCode = StatusCode.NotFound };

return new BaseResponse<ClientInformationModel> { Result = result };
```

### Step 5 — Verification checklist
After applying the fix:
- [ ] The bug scenario no longer reproduces
- [ ] No existing tests or API contracts broken
- [ ] All affected consuming services handle the change
- [ ] Cache invalidation still propagates if caching was involved
- [ ] Error is logged with structured parameters if exception path changed

## Output format

1. **Root cause** — one sentence.
2. **Files changed** — list with file paths.
3. **Diff** — complete before/after for every changed block.
4. **Verification steps** — what to test to confirm the fix.
5. **Follow-up items** — migrations, config changes, consumer updates required.
