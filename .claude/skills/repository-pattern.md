# Repository Pattern

Repository-specific knowledge for implementing and extending the data access layer in the Backoffice solution. Apply this when adding new repository methods, creating new repositories, or reviewing existing implementations.

---

## Repository locations

| Project | Repository directory | Role |
|---------|---------------------|------|
| `Backoffice.API` | `Repositories/` | Primary data access + business logic |
| `Backoffice.Config.Service` | `Repository/` | Config change processing |
| `Backoffice.Utility` | `Repository/` | Cache sync, activity log writes, exports |
| `Backoffice.WebSocket.Service` | (inline in service classes) | Minimal data access |

## Interface and implementation naming

```
IBackofficeRepository  →  BackofficeRepository
IClientRepository      →  ClientRepository
ITradingRepository     →  TradingRepository
ISportsbookRepository  →  SportsbookRepository
IPlatformRepository    →  PlatformRepository
IRabbitMqRepository    →  RabbitMqRepository
```

Every public method on `IXxxRepository` must be implemented in `XxxRepository`.

## Structural template

```csharp
public class XxxRepository : IXxxRepository
{
    #region Fields
    private readonly BackofficeContext _backofficeContext;
    private readonly IMemCache _memCache;
    private readonly IRabbitMqRepository _rabbitMqRepository;
    private readonly ILogger<XxxRepository> _logger;
    #endregion

    #region Ctor
    public XxxRepository(
        BackofficeContext backofficeContext,
        IMemCache memCache,
        IRabbitMqRepository rabbitMqRepository,
        ILogger<XxxRepository> logger)
    {
        _backofficeContext = backofficeContext;
        _memCache = memCache;
        _rabbitMqRepository = rabbitMqRepository;
        _logger = logger;
    }
    #endregion

    #region Public Methods

    public async Task<BaseResponse<T>> GetXxxAsync(int id)
    {
        // implementation
    }

    public async Task<BaseResponse<bool>> AddXxxAsync(AddXxxRequest request)
    {
        // implementation
    }

    #endregion

    #region Private Methods

    private async Task<bool> ValidateXxxAsync(int id)
    {
        // implementation
    }

    #endregion
}
```

## Return type rules

| Method type | Return type |
|-------------|------------|
| Fetch single entity | `Task<BaseResponse<EntityModel>>` |
| Fetch list | `Task<BaseResponse<List<EntityModel>>>` |
| Create / Update / Delete | `Task<BaseResponse<bool>>` |
| Fetch with count | `Task<BaseResponse<PagedResult<T>>>` |

Never return raw types. Every public method wraps its result in `BaseResponse<T>`.

## Error handling — required in every public method

```csharp
public async Task<BaseResponse<ClientInformationModel>> GetClientAsync(int clientId)
{
    try
    {
        var client = await _backofficeContext.ClientInformation
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClientId == clientId);

        if (client == null)
            return new BaseResponse<ClientInformationModel>
            {
                StatusCode = StatusCode.NotFound,
                Message = $"Client {clientId} not found"
            };

        return new BaseResponse<ClientInformationModel> { Result = client };
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to get Client {ClientId}: {Exception}", clientId, ex);
        return new BaseResponse<ClientInformationModel> { StatusCode = StatusCode.Error };
    }
}
```

Rules:
1. `try/catch` wraps the entire method body.
2. `catch` logs with structured parameters and returns `StatusCode.Error`.
3. Exceptions are never rethrown or propagated.
4. `NotFound` is a first-class return, not an exception.

## Dependency injection — constructor only

```csharp
// GOOD — injected
public XxxRepository(IYyyRepository yyyRepository)
{
    _yyyRepository = yyyRepository;
}

// BAD — instantiated directly
public void DoWork()
{
    var repo = new YyyRepository();  // Not testable, breaks DI lifecycle
}
```

All dependencies must be constructor-injected. Add new dependencies to both the constructor and the DI registration in `Startup.cs`.

## Method ordering convention

Within `#region Public Methods`, order methods as:

```
Get (single)
Get (list)
GetBy (filtered variants)
Add
AddOrUpdate
Update
Delete
```

This mirrors the CRUD sequence and makes large repositories (2000+ lines) navigable.

## BackofficeRepository scale

`BackofficeRepository.cs` is the largest file in the solution. When adding methods:
- Add to the correct `#region` — do not create new regions.
- Follow existing indentation and brace style exactly.
- Do not reorganise existing methods to make room.

## Multi-repository orchestration

When a repository method needs data from another repository, inject the dependency — do not create a new instance:

```csharp
public class BackofficeRepository : IBackofficeRepository
{
    private readonly ITradingRepository _tradingRepository;
    private readonly ISportsbookRepository _sportsbookRepository;

    // Use _tradingRepository.GetOddsEngineLeagueIdAsync() etc.
}
```

This is already the pattern in the existing `BackofficeRepository` constructor. Follow it.

## Repository vs. service distinction

In this codebase the repository layer is **conflated** with the service layer — repositories contain business logic. Do not extract a separate service class unless working in `Backoffice.Web` (which has an explicit service layer calling the API via `HttpClient`). In `Backoffice.API`, business logic belongs in the repository.
