# Authentication & Authorization

**Document Status:** Analysis Complete (No Code Changes Made)  
**Date:** June 18, 2026

## Authentication Architecture

### Three Authentication Mechanisms

```
┌─────────────────────────────────────────────┐
│         Incoming Request                    │
├─────────────────────────────────────────────┤
│  Headers:                                   │
│  - x-api-key (optional)                     │
│  - Authorization: Bearer {jwt} (optional)   │
│  - x-region (optional)                      │
└────────┬────────────────────────────────────┘
         │
    ┌────▼────┐
    │ Validate│
    │ Request │
    └────┬────┘
         │
    ┌────▼─────────────────────────────────┐
    │  Method 1: API Key Validation        │
    │  (ApiKeyFilter)                      │
    ├─────────────────────────────────────┤
    │  If x-api-key present:               │
    │  └─ Compare with config value        │
    │  └─ If valid → Continue              │
    │  └─ If invalid → 401 Unauthorized    │
    └────┬─────────────────────────────────┘
         │
    ┌────▼─────────────────────────────────┐
    │  Method 2: Bearer Token Validation   │
    │  (AuthorizeAttribute)                │
    ├─────────────────────────────────────┤
    │  If Authorization header present:    │
    │  └─ Extract Bearer token             │
    │  └─ Parse JWT                        │
    │  └─ Validate signature               │
    │  └─ Check expiration                 │
    │  └─ Extract claims                   │
    └────┬─────────────────────────────────┘
         │
    ┌────▼─────────────────────────────────┐
    │  Method 3: App Integration           │
    │  (Backoffice.Web Internal)           │
    ├─────────────────────────────────────┤
    │  If authenticated session:           │
    │  └─ Load user claims from session    │
    │  └─ Pass to API via Bearer           │
    └──────────────────────────────────────┘
```

---

## Authentication Flow: Web UI to API

### Step 1: User Login

```
User (Browser)
  ├─ Enters credentials
  └─ Clicks "Login"
       │
       ▼
Backoffice.Web (Blazor)
  ├─ Blazor form submission
  └─ HTTP POST /api/authentication/login
       │
       ├─ Header: x-api-key (web service key)
       └─ Body: { username, password }
```

### Step 2: Authentication Service Call

```
Backoffice.API (AuthenticationController)
  ├─ Receives login request
  ├─ Validates API key via ApiKeyFilter
  └─ Delegates to IAuthenticationRepository
       │
       ▼
AuthenticationRepository (IAuthenticationRepository)
  ├─ Creates HttpClient to Auth.API
  ├─ Includes API key: x-api-key header
  └─ POST /api/authentication/login
       │
       ├─ Credentials: { username, password }
       └─ Returns JWT token if valid
```

### Step 3: External Auth Service

```
Auth.API (External Service)
  ├─ Validates credentials
  ├─ Checks Active Directory / DB
  ├─ Generates JWT token
  └─ Returns: { token, expiresIn, ... }
```

### Step 4: Token Return to Client

```
Backoffice.API (AuthenticationController)
  ├─ Receives JWT from Auth.API
  └─ Returns to Backoffice.Web
       │
       ▼
Backoffice.Web (Blazor)
  ├─ Stores JWT in:
  │  ├─ Blazored.LocalStorage (persistent)
  │  └─ Session storage (temporary)
  │
  └─ Sets Authorization header for future requests:
     └─ Authorization: Bearer {jwt}
```

### Step 5: Subsequent API Calls

```
Backoffice.Web (Authenticated)
  ├─ All requests include:
  │  ├─ x-api-key (web service key)
  │  └─ Authorization: Bearer {jwt}
  │
  └─ HTTP GET /api/clients
       │
       ▼
Backoffice.API (ApiKeyFilter)
  ├─ Validates x-api-key
  └─ If valid → Continue
       │
       ▼
Backoffice.API (AuthorizeAttribute)
  ├─ Extracts JWT from Authorization header
  ├─ Validates signature (using JWT key)
  ├─ Checks expiration
  ├─ Extracts claims
  └─ If valid → Pass to controller
       │
       ▼
Controller Action
  ├─ Access to:
  │  ├─ Claims (user info)
  │  ├─ Permissions
  │  └─ Context
  │
  └─ Execute business logic
```

---

## API Key Authentication (ApiKeyFilter)

### Implementation

**Location:** `Backoffice.API/ActionFilter/ApiKeyFilter.cs`

```csharp
public class ApiKeyFilter : ActionFilterAttribute
{
    private readonly IConfiguration _configuration;

    public ApiKeyFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Extract API key from header
        var apiKey = context.HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
        
        // Compare with configured value
        if (string.IsNullOrEmpty(apiKey) || 
            _configuration["Security:ApiKey"] != apiKey)
        {
            context.Result = new BadRequestObjectResult(new
            {
                Title = "Unauthorized",
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "Missing Or Unsupported APIKey In Request Headers"
            });
            return;
        }
    }
}
```

### API Key Configuration

```json
{
    "Security": {
        "ApiKey": "9a8ad73dd129bf49754242e44988b3e7e39f0a05"
    }
}
```

**Issues:**
- ⚠️ API key in plaintext in config file
- ⚠️ Hardcoded comparison (case-sensitive)
- ⚠️ No hashing or rotation strategy

### Usage

```csharp
[ServiceFilter(typeof(ApiKeyFilter))]
public class ClientController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetClients()
    {
        // Only reached if API key is valid
    }
}
```

### API Key Sources

**Web (Backoffice.Web):**
```
x-api-key: web-api-key (from config)
```

**External Services:**
```
x-api-key: external-service-key (from config)
```

**Platform API:**
```
x-api-key: {region-specific-key}
e.g., API:PlatformAPI:HK:APIKey
```

---

## JWT Bearer Token Authentication

### JWT Token Structure

```
Header:
{
    "alg": "HS256",
    "typ": "JWT"
}

Payload:
{
    "iss": "bo.auth.api",
    "sub": "user123",
    "name": "John Doe",
    "email": "john@example.com",
    "claims": [
        "backoffice.clients.read",
        "backoffice.config.write",
        "backoffice.reports.export"
    ],
    "exp": 1687200000,  // Expiration time
    "iat": 1687199000   // Issued at
}

Signature:
HMACSHA256(
    base64UrlEncode(header) + "." +
    base64UrlEncode(payload),
    secret_key
)
```

### JWT Configuration

```json
{
    "API": {
        "AuthenticationAPI": {
            "JWT": {
                "Key": "07cae382c7303616f96e5f201b276a4f",
                "Issuer": "bo.auth.api"
            }
        }
    }
}
```

### JWT Validation (AuthorizeAttribute)

**Location:** `Backoffice.API/ActionFilter/AuthorizeAttribute.cs`

```csharp
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _claimType;
    private readonly int _claimValue;
    private readonly HttpClient _httpClient;

    public AuthorizeAttribute(string claimType = "", int claimValue = 0)
    {
        _claimType = claimType;
        _claimValue = claimValue;
        // Creates HttpClient to validate token via external API
    }

    public async void OnAuthorization(AuthorizationFilterContext context)
    {
        var region = context.HttpContext.Request.Headers["x-region"];
        var token = context.HttpContext.Request.Headers["Authorization"]
            .ToString()
            .Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
        {
            context.Result = new JsonResult(
                new { title = "Unauthorized", message = "Missing authorization" })
            { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        // Validate token via external API
        try
        {
            var response = await _httpClient.PostAsync(
                "/api/authentication/validatetoken",
                new StringContent(JsonSerializer.Serialize(new { token })));

            if (response.IsSuccessStatusCode)
            {
                // Extract claims from response
                var claims = JsonSerializer.Deserialize<List<Claim>>(
                    await response.Content.ReadAsStringAsync());

                // Attach to HttpContext
                var claimsIdentity = new ClaimsIdentity(claims, "jwt");
                context.HttpContext.User = new ClaimsPrincipal(claimsIdentity);
            }
            else
            {
                context.Result = new JsonResult(
                    new { title = "Unauthorized", message = "Invalid token" })
                { StatusCode = StatusCodes.Status401Unauthorized };
            }
        }
        catch (Exception ex)
        {
            context.Result = new JsonResult(
                new { title = "Error", message = ex.Message })
            { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }
}
```

### Usage

```csharp
[Authorize]  // Requires any valid JWT
public async Task<IActionResult> GetActivityLogs()
{
    // Access user claims: User.FindFirst("email")?.Value
}

[Authorize("backoffice.config.write", 1)]  // Requires specific claim
public async Task<IActionResult> UpdateConfig()
{
    // Only if user has this claim
}
```

### Issues with JWT Validation

1. **External API Call** - Makes HTTP call for every endpoint
2. **Synchronous** - Blocks request processing
3. **Async void** - Fire-and-forget, not properly awaited
4. **No Caching** - Token not cached, re-validated each time
5. **No Local Verification** - Could verify signature locally instead

### Outbound JWTs (Metabase SSO)

`Backoffice.Web` signs one JWT of its own: `MetabaseService.GetSsoToken` (HS256, signed with
`Metabase:SsoSecretKey`), served by `/sso/metabase` in `Program.cs`. Since BO-119
(`System.IdentityModel.Tokens.Jwt` 7.7.1) the key must be at least 32 bytes: a shorter key throws
`IDX10720` and the endpoint returns 500. The deployed keys are 64 characters.
`CommonService.GenerateToken` (Tableau JWT) has no callers; the Tableau embed uses trusted
tickets in `TableauService`.

Separately, the Metabase idle account cleanup (GM-987) calls the Metabase admin API with an API key
(`Metabase:ApiKey`, sent as `x-api-key`), not with a JWT. The key must belong to a Metabase administrator
because deactivating a user needs superuser rights, so treat it like the SSO secret: inject it from the
environment, never commit it and never log it (`MetabaseAdminClient` adds it to each request and never to a
shared client or an exception message).

---

## Authorization Patterns

### Pattern 1: Role-Based Access Control (RBAC)

```
User has Role(s):
├─ Admin
├─ ClientManager  
├─ Reporter
└─ User

Endpoint requires:
[Authorize] // Any authenticated user
[Authorize("Admin")] // Admin only
```

**Current Implementation:** Via claims, not explicit roles

### Pattern 2: Claim-Based Authorization

```csharp
[Authorize("backoffice.clients.read")]
[Authorize("backoffice.config.write")]
```

**Claims Examples:**
```
backoffice.activity.view
backoffice.clients.read
backoffice.clients.write
backoffice.config.read
backoffice.config.write
backoffice.reports.export
backoffice.settlement.approve
```

### Which API actions check a permission (BO-130)

The Web hides pages with policies (`Backoffice.Web/Extensions/WebApplicationBuilderExtensions.cs`), but a
policy only protects the screen. The API repeats the check with a method-level
`[Authorize("<claim>", (int)Access.X)]`. The claim name is the permission enum name with `_` replaced by `.`.
`Access.View` accepts a token value of 2 (View) or 1 (Edit). `Access.Edit` accepts only 1. A missing claim or 0
gives 401.

Stacked `[Authorize]` attributes all have to pass, so a class-level attribute must carry no claim when its
actions need different ones (see "Dedicated Translation Permission" in `08_TechnicalDebt.md`).

| Controller | Action | Claim | Access |
|---|---|---|---|
| BetController | `manualsettlement` | `Admin.Settlements.ManualBetSettlement` | Edit |
| BetController | `resendsettlement/details` | `Admin.Settlements.ResendSettlement` | View |
| BetController | `resendsettlement` | `Admin.Settlements.ResendSettlement` | Edit |
| BetController | `resendprovidercancellation` | `Admin.Settlements.ResendSettlement` | Edit |
| BetController | `cancel` | `Admin.CancelBets` | Edit |
| BetController | `details` | none (login only; shared by Manual Settlement, Market Settlement and Cancel Bets) | |

These three permissions have `HasReadOnly = 0`, so Manage User Groups can only switch them on (value 1) or off.
Super Admin receives value 1 for every permission outside category 99. Before adding a claim to another
action, check that the permission row exists in the Auth seed and is outside category 99, or Super Admin is
locked out. Translations, OddsEngine config templates, Client Site Config, Maintenance and Market Settlement
were already checked before BO-130. ClientImportController, ClientController, SettingsController and most of
TradingController still check only the login.

### Pattern 3: Resource-Based Authorization

```csharp
public async Task<IActionResult> GetClientConfig(int clientId)
{
    // Verify user has access to this specific client
    if (!User.CanAccess(clientId))
        return Forbid();
    
    // Proceed with operation
}
```

**Currently Not Implemented**

---

## Session Management

### Backoffice.Web Session

```csharp
// Session middleware configured
builder.Services.AddSession();
app.UseSession();
```

**Session Storage:** In-memory (default)

**Issues:**
- ✗ Not distributed
- ✗ Lost on app restart
- ✗ Not scalable

**Better Approach:** Move to Redis

### Session Data

```csharp
// Typically stores
HttpContext.Session["user_id"] = userId;
HttpContext.Session["roles"] = roles;
HttpContext.Session["last_activity"] = DateTime.UtcNow;
```

### Local Storage (Blazor)

```csharp
// Persistent browser storage
await localStorage.SetItemAsync("auth_token", jwt);
await localStorage.SetItemAsync("user_preferences", prefs);

// Retrieved on app startup
var token = await localStorage.GetItemAsync<string>("auth_token");
```

---

## Cross-Service Authentication

### Service-to-Service

```
Backoffice.API → Platform API
├─ Includes: x-api-key header
└─ Platform validates API key (not user)

Backoffice.API → Sportsbook API
├─ Includes: x-api-key header
└─ Sportsbook validates API key

Backoffice.API → Auth API
├─ Includes: x-api-key header
├─ Plus: Bearer token to validate
└─ Auth API validates both
```

**Pattern:** API-key for service identity, Bearer token for user identity

---

## Security Issues

### Critical Issues

1. **API Key in Configuration** 
   - Should be in secrets manager
   - Should be rotated regularly

2. **Bearer Token Validation via External Call**
   - Slow (HTTP round-trip)
   - Single point of failure
   - Better: Local JWT validation

3. **No Token Caching**
   - Token validated on every request
   - Consider: Cache validated tokens

4. **Fire-and-Forget Validation**
   - `async void OnAuthorization`
   - Errors not properly propagated

### High Priority Issues

1. **No Rate Limiting**
   - No protection against brute force
   - No token refresh limits

2. **No Token Revocation**
   - Issued tokens always valid
   - Can't revoke compromised tokens

3. **No Multi-factor Authentication (MFA)**
   - Only username/password

4. **No Audit Logging**
   - Failed auth attempts not tracked
   - No security event log

---

## Authorization Gaps

### Missing Checks

1. **No Resource-Level Authorization**
   - Can't restrict user to their own clients
   - All users see all data

2. **No Row-Level Security (RLS)**
   - No database-level restrictions
   - Relies on API to enforce

3. **No Scope-Based Limitations**
   - Tokens don't limit scope
   - All endpoints available

4. **No IP Whitelisting**
   - Any source can authenticate

---

## Authentication Flow Diagram

```
┌──────────────────────────────────────────────────────┐
│                 Initial Login                        │
├──────────────────────────────────────────────────────┤
│ 1. User enters credentials                          │
│ 2. Browser → Blazor: Submit login form              │
│ 3. Blazor → API: POST /login (x-api-key)            │
│ 4. API → Auth API: Validate credentials             │
│ 5. Auth API → API: Return JWT                       │
│ 6. API → Blazor: Return JWT                         │
│ 7. Blazor: Store JWT in LocalStorage & Session      │
│ 8. Blazor: Redirect to dashboard                    │
└──────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────┐
│            Authenticated API Calls                  │
├──────────────────────────────────────────────────────┤
│ 1. Blazor → API: GET /api/clients                   │
│    Headers:                                         │
│    - x-api-key: web-api-key                        │
│    - Authorization: Bearer {jwt}                    │
│    - x-region: HK                                   │
│                                                     │
│ 2. API: ApiKeyFilter validates x-api-key           │
│ 3. API: AuthorizeAttribute validates JWT            │
│    3a. Calls Auth API to validate token            │
│    3b. Extracts claims from response               │
│                                                     │
│ 4. API: Controller executes (user context set)     │
│ 5. API: Returns response (200 OK)                  │
│                                                     │
│ 6. Blazor: Receives response                       │
│ 7. Blazor: Updates UI with data                    │
└──────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────┐
│              Token Expiration                       │
├──────────────────────────────────────────────────────┤
│ 1. JWT has exp claim (e.g., 1 hour)                │
│ 2. Time passes, token expires                      │
│ 3. Next API call fails with 401 Unauthorized       │
│ 4. Blazor catches 401                              │
│ 5. Blazor redirects to login                       │
│ 6. User must re-authenticate                       │
└──────────────────────────────────────────────────────┘
```

---

## Recommendations

### Immediate (Critical)

1. Move API keys to secrets manager
2. Add local JWT validation (don't call external API)
3. Implement token caching
4. Fix async void issue in AuthorizeAttribute

### Short-term (High Priority)

1. Add rate limiting
2. Implement token revocation
3. Add audit logging for failed attempts
4. Implement resource-level authorization

### Medium-term

1. Add multi-factor authentication (MFA)
2. Implement row-level security (RLS)
3. Add IP whitelisting for sensitive endpoints
4. Implement token refresh logic

### Long-term

1. Consider OAuth2 / OpenID Connect
2. Implement Single Sign-On (SSO) integration
3. Add federated identity support
4. Implement certificate pinning for service-to-service

