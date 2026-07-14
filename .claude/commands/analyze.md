# /analyze

Analyze a file, class, repository, or feature area in the Backoffice solution. Produce a structured report covering responsibilities, dependencies, data flow, and any concerns.

## Usage

```
/analyze $ARGUMENTS
```

`$ARGUMENTS` can be:
- A file path (e.g. `Backoffice.API/Repositories/BackofficeRepository.cs`)
- A class or interface name (e.g. `IBackofficeRepository`)
- A feature area (e.g. `client configuration`, `cache invalidation`)
- A project name (e.g. `Backoffice.Config.Service`)

## What to produce

### 1. Summary
One paragraph describing the purpose and scope of what was analyzed.

### 2. Responsibilities
Bullet list of what this component owns. Keep each item to one line.

### 3. Dependencies
List injected services, DbContexts, and external systems (RabbitMQ, MongoDB, SQL). For each dependency note *why* it is needed.

### 4. Data Flow
Trace the path data takes: incoming request/message → repository → database/cache/broker → response/event. Use an ASCII diagram if the flow is non-trivial.

### 5. Patterns in Use
Identify which codebase conventions are applied:
- `BaseResponse<T>` return type
- Try/catch with `_logger.LogError`
- `AsNoTracking()` for read queries
- `IMemCache` usage
- RabbitMQ publish/consume
- `TransactionScope` for multi-context writes

### 6. Concerns
Flag anything that deviates from CLAUDE.md standards or represents known technical debt:
- Synchronous calls where async is expected
- Missing `AsNoTracking()`
- Fire-and-forget message publishing without error handling
- Static/non-thread-safe initialization (e.g. MongoDB singleton)
- Missing `[Authorize]` or `[ApiKey]` on controller actions
- Repository methods returning raw types instead of `BaseResponse<T>`

### 7. Recommendations
Short, actionable list. Each item must be scoped — no broad refactor suggestions unless the user asked for them. Reference CLAUDE.md section if relevant.

## Constraints
- Do NOT suggest refactors outside the analyzed scope.
- Do NOT modify any files.
- Keep the report factual; cite file paths and line numbers for every concern raised.
