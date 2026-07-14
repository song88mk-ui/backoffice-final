# /api

Design or extend a REST API endpoint in Backoffice.API following CLAUDE.md conventions.

## Usage

```
/api $ARGUMENTS
```

`$ARGUMENTS` examples:
- `GET /api/client/{clientId}/betconfig`
- `POST /api/config/template — add new config template`
- `DELETE /api/client/{clientId}/margin/{marginId}`
- `list all endpoints in ClientController`

## Endpoint design checklist

### Authorization
Every endpoint must declare one:
- `[Authorize]` — requires valid JWT bearer token (used for UI-facing calls)
- `[ApiKey]` — requires `x-api-key` header (used for internal service-to-service calls)

Do not mix both on the same action unless explicitly required.

### Route conventions
```
GET    /api/{entity}/{id}           — fetch single
GET    /api/{entity}                — fetch list
POST   /api/{entity}                — create
PUT    /api/{entity}/{id}           — full update
PATCH  /api/{entity}/{id}           — partial update
DELETE /api/{entity}/{id}           — delete
```

### Request DTOs (`Backoffice.Shared/Models/Requests/`)
```csharp
public class AddXxxRequest
{
    [Required]
    public int ClientId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Range(1, int.MaxValue)]
    public int MinimumBet { get; set; }
}
```

Rules:
- Validate at model level using data annotations.
- Name pattern: `AddXxxRequest`, `UpdateXxxRequest`, `DeleteXxxRequest`.
- Never expose internal IDs or database keys directly if they can be enumerated.

### Response envelope
All endpoints return `BaseResponse<T>`:
```csharp
public class BaseResponse<T>
{
    public T Result { get; set; }
    public StatusCode StatusCode { get; set; }
    public string Message { get; set; }
}
```

HTTP status mapping:
```
StatusCode.Success          → HTTP 200 OK
StatusCode.ValidationError  → HTTP 200 OK (envelope carries error)
StatusCode.NotFound         → HTTP 200 OK (envelope carries error)
StatusCode.Error            → HTTP 200 OK (envelope carries error)
```

Note: This codebase returns HTTP 200 in all cases; the `StatusCode` field in the envelope indicates outcome.

### Controller template
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    [ApiKey]
    public async Task<IActionResult> AddXxx([FromBody] AddXxxRequest request)
    {
        var response = await _repository.AddXxxAsync(request);
        return Ok(response);
    }
}
```

### Swagger documentation
Add XML comments to public action methods:
```csharp
/// <summary>Returns the bet configuration for a specific client.</summary>
/// <param name="clientId">The client identifier.</param>
[HttpGet("{clientId}/betconfig")]
public async Task<IActionResult> GetBetConfig(int clientId) { }
```

### DI registration
Register in `Backoffice.API/Startup.cs`:
```csharp
services.AddScoped<IXxxRepository, XxxRepository>();
```

## Output format

Produce:
1. The complete new or modified controller file.
2. Any new request/response DTOs.
3. The repository interface addition.
4. The repository implementation.
5. The Startup.cs registration line.
6. Any follow-up items (migrations, message types, cache keys).
