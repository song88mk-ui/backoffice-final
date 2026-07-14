# /review

Review staged or recent changes against the Backoffice CLAUDE.md standards. Produce a structured findings report.

## Usage

```
/review $ARGUMENTS
```

`$ARGUMENTS` options:
- (empty) — review current git diff (staged + unstaged)
- `staged` — review only staged changes
- A file path — review that specific file
- A PR number — review that pull request's diff

## Review dimensions

Check every dimension below. Skip dimensions with no relevant changes.

### 1. Architecture & Pattern Compliance
- [ ] All new repository methods return `Task<BaseResponse<T>>`
- [ ] No `new XxxRepository()` or `new XxxService()` inside methods
- [ ] Constructor injection used for all dependencies
- [ ] Repositories implement their `IXxxRepository` interface
- [ ] Controller actions stay thin (no business logic inline)

### 2. Database & EF Core
- [ ] Every read query uses `.AsNoTracking()`
- [ ] Navigation properties loaded with `.Include()` before use
- [ ] No raw SQL strings (EF Core only, per CLAUDE.md)
- [ ] Multi-context writes wrapped in `TransactionScope(TransactionScopeAsyncFlowOption.Enabled)`
- [ ] No `.ToList()` — must be `.ToListAsync()`

### 3. Error Handling & Logging
- [ ] Every public repository method has `try { } catch (Exception ex) { }`
- [ ] `_logger.LogError(...)` called with **named parameters** (structured logging)
- [ ] No stack traces, passwords, API keys, or PII logged
- [ ] Exception caught and returned as `BaseResponse { StatusCode = StatusCode.Error }`, not rethrown

### 4. Caching
- [ ] Cache keys follow `{entity}.{operation}.{identifier}` naming
- [ ] Mutations invalidate cache via both `_memCache.DeleteCache` **and** RabbitMQ `OnDeleteCacheMessage`
- [ ] Cache TTL is appropriate for data volatility (1–24 hours)

### 5. Messaging
- [ ] New message classes added to `Backoffice.Shared/Models/OutgoingEngineMessages/`
- [ ] All published messages include a `CorrelationId`
- [ ] All consuming services handle any new message types
- [ ] No breaking changes to existing message schemas (only additive changes)

### 6. API Contracts
- [ ] No parameters removed or renamed from existing endpoints
- [ ] No response field removals (additions are fine)
- [ ] `[Authorize]` or `[ApiKey]` present on every new controller action
- [ ] New endpoints return `Ok(BaseResponse<T>)` consistently

### 7. Security
- [ ] No SQL injection vectors (EF only)
- [ ] No hardcoded secrets or connection strings
- [ ] Sensitive data absent from logs
- [ ] Authorization attributes applied

### 8. Naming Conventions
- [ ] Classes/interfaces: `IXxxRepository` / `XxxRepository`, `XxxModel`, `XxxRequest`, `XxxResponse`, `OnXxxMessage`
- [ ] Async methods suffixed `Async`
- [ ] Private fields: `_camelCase`
- [ ] `#region` blocks used in large classes (Fields / Ctor / Public Methods / Private Methods)

## Output format

For each failing check, output:

```
[SEVERITY] File:Line — description of issue
```

Severity levels:
- `[BLOCKER]` — violates a hard rule from CLAUDE.md (must fix before merge)
- `[WARNING]` — deviates from convention or introduces known technical debt
- `[INFO]` — style suggestion, non-critical

End the report with a summary: total blockers / warnings / infos. If all checks pass, say so explicitly.
