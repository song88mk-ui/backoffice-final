---
name: api-development
description: How HTTP endpoints in Backoffice.API are built: MVC controllers, routes, the x-api-key and JWT authorization attributes, BaseResponse and StatusCode, and request DTOs. Use when adding or changing a controller action, a route, authorization, or a request or response shape.
---

# API Development

Repository-specific knowledge for building and extending HTTP endpoints in `Backoffice.API` (MVC controllers, `net10.0`, `Startup.cs` hosting). Apply this when adding or changing a controller action, choosing a route, wiring authorization, or shaping a request or response. Paths below are relative to `backoffice-final/`.

---

## Controller location and structure

There are 10 controllers under `Backoffice.API/Controllers/`, grouped by folder:

```
Authentication/     AuthenticationController   api/authentication    login, users, user groups, permissions
Backoffice/         AdminConfigController      api/config            maintenance flags
                    BetController              api/bet               bet details, manual settlement, resend settlement, cancel
                    ConfigController           api/config            config templates, margins, activity logs, cache preload
                    TranslationController      api/translation       static/dynamic sportsbook translations
                    WebPropsController         api/config/feature-flags   Client Site Config (webprops-api)
SportsbookClients/  ClientController           sportsbook/client     clients, bet settings, market template groups, sort orders
                    ClientImportController     sportsbook/clientimport    client import validate/commit
                    SettingsController         sportsbook/settings   top fixtures, top leagues, parlay bonus, announcements
Trading/            TradingController          api/trading           fixtures, markets, mappings, settlements (1239 lines, 73 actions)
```

There is no `SportsbookController`. Most controllers use `[Route("api/[controller]")]` or `[Route("sportsbook/[controller]")]`; `AdminConfigController` and `WebPropsController` use literal routes (`Controllers/Backoffice/AdminConfigController.cs:17`, `Controllers/Backoffice/WebPropsController.cs:14`). Add to an existing controller unless the permission claim differs (see "Class-level vs method-level authorization" below).

## Controller template

A real, complete controller (`Backoffice.API/Controllers/Backoffice/WebPropsController.cs:13-39`, trimmed):

```csharp
using AuthorizeAttribute = Backoffice.API.ActionFilter.AuthorizeAttribute;
using BoStatusCode = Backoffice.Shared.Enums.StatusCode;

[Authorize("Admin.ClientSiteConfig", ((int)Access.View))]
[Route("api/config/feature-flags")]
[ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
public class WebPropsController : ControllerBase
{
    private readonly IWebPropsRepository _webPropsRepository;

    public WebPropsController(IWebPropsRepository webPropsRepository)
    {
        _webPropsRepository = webPropsRepository;
    }

    [HttpGet("{clientCode}")]
    public async Task<IActionResult> GetClientFeatureFlags(string clientCode)
    {
        var result = await _webPropsRepository.GetClientFeatureFlagsAsync(clientCode);

        switch (result.StatusCode)
        {
            case BoStatusCode.Success:
                return Ok(result);
            case BoStatusCode.NotFound:
                return NotFound(result);
            default:
                return BadRequest(result);
        }
    }
}
```

Conventions visible across controllers:
- Every controller except `AuthenticationController` carries `[ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]`.
- Controllers are mostly thin: call one repository method and map `StatusCode` to an HTTP result. A few do more (JWT claim parsing in `ClientController.GetClientList`, request checks in `ConfigController.CachePreload`). No controller injects `ILogger`.
- `ClientController` and `ClientImportController` derive from `Controller`; the rest derive from `ControllerBase`.
- The `using AuthorizeAttribute = Backoffice.API.ActionFilter.AuthorizeAttribute;` alias appears in several files (`ConfigController.cs:19`, `WebPropsController.cs:8`). Keep it so `[Authorize]` can never resolve to ASP.NET Core's attribute.

## Authorization model

Two custom filters, both in `Backoffice.API/ActionFilter/`. Neither is ASP.NET Core's authentication/authorization stack (`Startup.cs:141` calls `UseAuthorization()` but no scheme or policy is registered).

| Filter | Applied as | Checks |
|--------|-----------|--------|
| `ApiKeyFilter` (`ActionFilterAttribute`) | class-level `ServiceFilter(typeof(ApiKeyFilter))`, registered with `services.AddTransient<ApiKeyFilter>()` (`Startup.cs:125`) | `x-api-key` header equals `Security:ApiKey` config (`ApiKeyFilter.cs:20-23`) |
| `AuthorizeAttribute` (`IAuthorizationFilter`) | `[Authorize]` or `[Authorize("<Claim>", (int)Access.View\|Edit)]`, class or method | Bearer JWT, then the Auth API, then an optional permission claim |

`ApiKeyFilter` details: it reads the Bearer token (`ApiKeyFilter.cs:21`) but never uses it. On failure it returns HTTP 400 (`BadRequestObjectResult`) whose body says `StatusCode = 401` (`ApiKeyFilter.cs:25-30`).

`AuthorizeAttribute.OnAuthorization` (`ActionFilter/AuthorizeAttribute.cs:45`) in order:
1. Stores `x-language-code` in `HttpContext.Items["LanguageCode"]` (`:53-54`).
2. Returns early if the action has the custom `AllowAnonymousAttribute` (`:56`, defined at `:178`).
3. Requires an `Authorization` header (`:59`), checks the JWT issuer against `API:AuthenticationAPI:JWT:Issuer` (`:77`) and expiry (`:83`).
4. Calls the Auth API `POST v1/authentication/access` with the token (`:95`, `:145`). `OnAuthorization` is declared `async void` but blocks on `Authenticate(token).Result`, so every protected request makes a synchronous HTTP round trip.
5. If a claim was supplied (`:104`): the JWT must contain that claim type (case-insensitive); value `NoAccess` is rejected (`:114`); an `Edit` requirement rejects a `View` value (`:119`). `Access` is `NoAccess = 0, Edit = 1, View = 2` (`Backoffice.Shared/Enums/Access.cs`).
6. Populates `HttpContext.Items` `UserToken`, `UserId`, `UserName`, `UserIdRef`, `ClientCodes` (`:126-130`).

All failures return HTTP 401 with `{ title, message }`. Claims used today: `SBC.OE.Config`, `Translations.Static`, `Translations.Dynamic`, `Admin.Maintenance`, `Admin.ClientSiteConfig`, `Admin.Settlements.MarketSettlement`. There is no `Roles =` usage anywhere.

### Class-level vs method-level authorization

The custom attribute does not merge like ASP.NET Core policies: a class-level and a method-level instance both run, class first. `TranslationController.cs:16-24` documents why those actions were moved off `ConfigController`: a user lacking the class-level claim was rejected before the method-level claim was checked. That comment says `ConfigController` carries `[Authorize("SBC.OE.Config", View)]`, but today its class-level attribute is a bare `[Authorize]` (`ConfigController.cs:24`); the comment is stale on that detail, the ordering rule still holds. If an action needs a different claim than its controller's class-level one, put it on a controller without a restrictive class-level `[Authorize]`.

### Anonymous and unfiltered endpoints

- `[AllowAnonymous]` (the custom one) is used on `ConfigController` `cachepreload` and `translations` (`ConfigController.cs:302`, `:346`). It skips only `AuthorizeAttribute`; `ApiKeyFilter` still runs.
- `AuthenticationController` has no `ApiKeyFilter` and no `[Authorize]` at all (`Controllers/Authentication/AuthenticationController.cs:18-20`); it forwards credentials and tokens to the Auth API via `IAuthenticationRepository`.

## Response envelope: BaseResponse\<T\>

`Backoffice.Shared/Models/BaseResponse.cs:5-10`:

```csharp
public class BaseResponse<T>
{
    public StatusCode StatusCode { get; set; }
    public string Message { get; set; }
    public T Result { get; set; }
}
```

`Backoffice.Shared/Enums/StatusCode.cs:3-14`: `Success = 100`, `Missing_Or_Unsupported_APIKey_In_Request_Headers = 101`, `Rejected_Bet_Are_Not_Allowed_To_Be_Resent_To_Operator = 150`, `Resend_Attempt_Not_Successful_Please_Try_Again = 151`, `Invalid_Password = 201`, `Username_Not_Found = 202`, `Unauthorized = 401`, `NotFound = 404`, `Error = 1000`.

HTTP status is not always 200. The standard action body is `Success` to `Ok(result)`, anything else to `BadRequest(result)` (HTTP 400), as in `ClientController.cs:30-42`. `WebPropsController` also maps `NotFound` to `NotFound(result)`. Repositories catch exceptions and return `StatusCode.Error` with `$"{StatusCode.Error...} ({ex.Message})"` instead of throwing (e.g. `Repositories/BackOfficeRepository.cs` `GetClients`). `ConfigController.CachePreload` is an exception: it returns an anonymous `{ Success, Message }` object and plain-string `BadRequest`s.

JSON: `Startup.cs:79-82` adds `JsonStringEnumConverter` to System.Text.Json options, and `Startup.cs:126` later calls `AddNewtonsoftJson` (with only `ReferenceLoopHandling.Ignore`). `AddNewtonsoftJson` normally replaces the System.Text.Json MVC formatters, which would make the enum converter dead and send `StatusCode` as a number. Unverified: not confirmed at runtime.

## Request DTO location and naming

81 request classes live in `Backoffice.Shared/Models/Requests/`. Names are mostly `<Thing>Request`, with prefixes such as `AddOrUpdate`, `Get`, `Delete`, `Copy`, `Post` (e.g. `AddOrUpdateBetConfigRequest`, `GetClientListRequest`, `DeleteAnnouncementRequest`). Some actions bind response or model types directly, e.g. `ClientController.AddOrUpdateClient([FromBody] ClientListResponse.Client request)` (`ClientController.cs:69`) and `ConfigController.ActivityLog([FromBody] ActivityLogModel request)`.

No request class uses data annotations (`[Required]`, `[Range]`, `[StringLength]`, `[RegularExpression]`): grep finds none. Validation is done in repositories and returned as `StatusCode.Error` with a message, e.g. `BackOfficeRepository.AddOrUpdateClient` rejects duplicate brand, duplicate name, and non-`https://` websites (`Repositories/BackOfficeRepository.cs:605-631`; the website check is `Contains("https://")`, not a prefix check).

## Route conventions

Routes are verb-style path segments, not REST resources. Of 174 `[Http*]` attributes, all are `HttpGet` or `HttpPost` except one: `[HttpDelete("mapping/sports")]` (`Controllers/Trading/TradingController.cs:918`). There is no `HttpPut` or `HttpPatch`. Real examples:

```
GET  sportsbook/client/list                      ClientController.cs:30
POST sportsbook/client/information/list          ClientController.cs:44
POST sportsbook/client/post                      ClientController.cs:68  (add or update)
POST sportsbook/client/markettemplategroup/delete  ClientController.cs:154
GET  api/config/template/{id}                    ConfigController.cs:37
POST api/config/template/delete/{id}             ConfigController.cs:104
POST api/authentication/usergroup/post           AuthenticationController.cs:313
```

Follow the pattern for new actions: `.../list` for queries with a body, `.../post` for add-or-update, `.../delete` as a POST. List queries usually take a POST body even when read-only.

Request headers read by controllers:
- `x-region`: read in 80 places (72 in `TradingController`, 5 in `BetController`, 3 in `ClientController`) and passed down as `region`.
- `x-page`: becomes `RequesterInfo.Page` for activity logs (`ConfigController.cs:111-116`, `Backoffice.API/Models/RequesterInfo.cs`).
- `x-language-code`: see step 1 of the authorization flow above.

Brand scoping: `ClientController.GetClientList` parses the `ClientCodes` JWT claim; `ALL` sees every client, otherwise a comma list of brands filters the query (`ClientController.cs:47-56`).

## Swagger documentation

Swashbuckle.AspNetCore 6.4.0 (`BackOffice.API.csproj:12`). `Startup.cs:54-76` registers doc `v1` ("Backoffice API") with an `x-api-key` header security scheme; there is no Bearer scheme. `UseSwagger()` and `UseSwaggerUI()` run in every environment (`Startup.cs:137-138`). No controller has XML `/// <summary>` comments and XML doc generation is not enabled in the csproj.

## DI registration in Startup.cs

All registrations are in `Startup.ConfigureServices` (`Backoffice.API/Startup.cs:34-128`):
- Plain repositories are `AddScoped` (`Startup.cs:105`, `:119-123`).
- Repositories that call HTTP services are typed clients: `AddHttpClient<IAuthenticationRepository, AuthenticationRepository>` (`:84-89`), `IMaintenanceRepository` and `IWebPropsRepository` against `API:WebProps:Uri` with an `x-api-key` header (`:108-117`), and `IRabbitMqRepository` (`:104`).
- `IMemCache` is a singleton (`:124`).
- DbContexts: `SportsbookContext` and `TradingContext` via `AddDbContext`, `BackofficeContext` and `TranslationContext` via `AddDbContextPool`, all with `Custom:DbContextTimeout` (default 180s) (`:91-102`).

Known exceptions to the factory pattern: `RabbitMqRepository` discards its injected `HttpClient` (constructor at `Repositories/RabbitMQRepository.cs:25`) and creates `new HttpClient()` per publish (`:85`). Despite its name it does not use `RabbitMQ.Client`: it POSTs to a broker service at `v1/broker/publish` (`:37`), with the base URL chosen by region. `RabbitMQ.Client` 5.2.0 is referenced only by `Backoffice.Shared` and `Backoffice.Web`; `AuthorizeAttribute` creates its own `HttpClient` and reads `appsettings.json` directly (`ActionFilter/AuthorizeAttribute.cs:25-28`, `:39`). Do not copy either pattern into new code.

## Logging

Controllers do not log. Repositories do, in two styles: Serilog's static `Log.Error(...)` (`BackOfficeRepository.cs`, e.g. platform registration failures in `AddOrUpdateClient`) and injected `ILogger<T>` (`WebPropsRepository`, `TradingRepository`). Match the style already used in the repository you are editing.

## Tests

`Backoffice.API.Tests` (xUnit 2.4.2, Moq 4.18.4, `Backoffice.API.Tests.csproj:10-12`; note this repo pins 2.4.2, not the 2.5.3 used elsewhere in the workspace, and `Backoffice.Web.Tests` also pins 2.4.2) has folders `ActivityLog`, `IncomingSbEngineMessages`, `Maintenance`, `MarketSettlement`, `Translation`, `WebProps`. No test targets `ApiKeyFilter` or `AuthorizeAttribute`, and there is no `WebApplicationFactory` test of the API pipeline. Run with `dotnet test backoffice-final/Backoffice.API.Tests/Backoffice.API.Tests.csproj` from the workspace root.

## Backward compatibility rules

`Backoffice.Web` calls these routes by literal string (e.g. `Backoffice.Web/Services/BackOfficeService.cs:167` uses `api/config/feature-flags/{clientCode}`, `:349` uses `api/config/activitylog`). Renaming a route, a route parameter, or a request property breaks the UI with no compile error. Adding optional properties to a request or new properties to a response is safe.
