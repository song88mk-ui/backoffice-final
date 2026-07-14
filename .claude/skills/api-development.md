# API Development

Repository-specific knowledge for building and extending REST endpoints in `Backoffice.API`. Apply this when adding controllers, designing request/response shapes, or configuring authorization.

---

## Controller location and structure

Controllers live in `Backoffice.API/Controllers/`. Each controller owns one domain area:

```
ClientController        — client CRUD, bet settings
ConfigController        — config templates, margins
TradingController       — fixture/market/selection lookups
SportsbookController    — sportsbook data queries
TranslationController   — translation management
```

Use existing controllers before creating new ones. Only create a new controller if the domain is genuinely distinct.

## Controller template

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Default: JWT required for all actions
public class XxxController : ControllerBase
{
    #region Fields
    private readonly IXxxRepository _repository;
    private readonly ILogger<XxxController> _logger;
    #endregion

    #region Ctor
    public XxxController(IXxxRepository repository, ILogger<XxxController> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    #endregion

    [HttpGet("{id}")]
    public async Task<IActionResult> GetXxx(int id)
    {
        var response = await _repository.GetXxxAsync(id);
        return Ok(response);
    }

    [HttpPost]
    [ApiKey]  // Override: API key required (internal callers)
    public async Task<IActionResult> AddXxx([FromBody] AddXxxRequest request)
    {
        var response = await _repository.AddXxxAsync(request);
        return Ok(response);
    }
}
```

## Authorization model

Two attributes, both defined in `Backoffice.API/ActionFilter/`:

| Attribute | Filter class | Validates | Used for |
|-----------|-------------|-----------|---------|
| `[Authorize]` | `AuthorizeAttribute.cs` | JWT bearer token | UI-facing endpoints (browser calls) |
| `[ApiKey]` | `ApiKeyFilter.cs` | `x-api-key` header against appsettings | Internal service-to-service calls |

Rules:
- `[Authorize]` at the class level covers all actions — override with `[ApiKey]` on specific actions if needed.
- **Never leave an endpoint with no authorization.** Every action must have at least one.
- `[AllowAnonymous]` must not be used without explicit sign-off.

## Response envelope — BaseResponse\<T\>

```csharp
public class BaseResponse<T>
{
    public T Result { get; set; }
    public StatusCode StatusCode { get; set; }
    public string Message { get; set; }
}

public enum StatusCode
{
    Success = 1,
    Error = 2,
    ValidationError = 3,
    NotFound = 4,
    Unauthorized = 5
}
```

All actions return `Ok(response)` where `response` is a `BaseResponse<T>`. The HTTP status code is always 200 — error state is indicated by `StatusCode` in the envelope.

```csharp
// GOOD
return Ok(await _repository.GetXxxAsync(id));

// BAD — raw data, not wrapped
return Ok(await _repository.GetXxxAsync(id).Result);

// BAD — wrong HTTP status code
return NotFound();
```

## Request DTO location and naming

Request DTOs live in `Backoffice.Shared/Models/Requests/`:

```
AddXxxRequest
UpdateXxxRequest
DeleteXxxRequest
GetXxxRequest   (when query parameters are complex)
```

Validation via data annotations:

```csharp
public class AddClientRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string ClientName { get; set; }

    [Required]
    [RegularExpression("^[A-Z]{2}$", ErrorMessage = "RegionCode must be 2 uppercase letters")]
    public string RegionCode { get; set; }

    [Range(0, 10000)]
    public int DefaultMargin { get; set; }
}
```

Model validation runs automatically via `[ApiController]` — no manual `ModelState.IsValid` check needed.

## Route conventions

```
GET    /api/client/{clientId}                    — single client
GET    /api/client                               — client list
GET    /api/client/{clientId}/betconfig          — nested resource
POST   /api/client                               — create
PUT    /api/client/{clientId}                    — full update
PATCH  /api/client/{clientId}/margin             — partial update
DELETE /api/client/{clientId}                    — delete
```

Use `[FromRoute]` for path parameters (default when param matches route token). Use `[FromBody]` explicitly on POST/PUT bodies.

## Swagger documentation

Add XML summary to all public action methods:

```csharp
/// <summary>Returns active bet configuration for a client.</summary>
/// <param name="clientId">Backoffice client identifier.</param>
[HttpGet("{clientId}/betconfig")]
public async Task<IActionResult> GetBetConfig(int clientId) { }
```

## DI registration in Startup.cs

Register new repositories as **Scoped** (one per HTTP request):

```csharp
// Backoffice.API/Startup.cs — ConfigureServices method
services.AddScoped<IXxxRepository, XxxRepository>();
```

Register `HttpClient` with the factory pattern — never `new HttpClient()`:

```csharp
services.AddHttpClient<IExternalService, ExternalService>(client =>
{
    client.BaseAddress = new Uri(Configuration["ExternalApi:BaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

## Logging in controllers

Controllers should not log business events — those belong in the repository. Controllers log only infrastructure events (e.g. auth failure, unexpected exception that escapes the repository):

```csharp
catch (Exception ex)
{
    _logger.LogError("Unexpected error in XxxController.GetXxx {Id}: {Exception}", id, ex);
    return StatusCode(500, new BaseResponse<T> { StatusCode = StatusCode.Error });
}
```

## Backward compatibility rules

Never remove or rename:
- Route paths (`/api/client` → `/api/clients` is a breaking change)
- Action method parameters
- Required fields in request DTOs
- Fields in `BaseResponse<T>` subclasses

Adding optional fields to request DTOs and adding new response fields are both safe.
