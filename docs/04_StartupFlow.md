# Startup Flow & Initialization

**Document Status:** Analysis Complete (No Code Changes Made)  
**Date:** June 18, 2026

## Backoffice.API Startup Sequence

### 1. Program.cs Execution
```
Main(string[] args)
  ↓
CreateHostBuilder(args).Build().Run()
  ├─ Host.CreateDefaultBuilder(args)
  ├─ UseSerilog((context, _, serilog) => {...})
  │   └─ Configure Serilog from configuration
  │
  └─ ConfigureWebHostDefaults(webBuilder => {
      webBuilder.UseStartup<Startup>();
    })
```

### 2. Startup.ConfigureServices

#### Step 1: Telemetry & Instrumentation
```csharp
services.AddSingleton<Instrumentation>();
services.AddTelemetry(new TelemetrySettings(
    ServiceName: Instrumentation.ActivitySourceName,
    ActivitySourceName: Instrumentation.ActivitySourceName,
    MeterName: Instrumentation.MeterName));
```
**Outcome:** ActivitySource, Counter, Meter initialized

#### Step 2: MVC & Serialization
```csharp
services.AddControllers();
services.AddControllers().AddJsonOptions(opt =>
{
    opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
services.AddControllers().AddNewtonsoftJson(options => 
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);
```
**Outcome:** Controllers registered, JSON serialization configured

#### Step 3: CORS Policy
```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin", o => o.AllowAnyOrigin());
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
```
**Outcome:** Two permissive CORS policies (WARNING: Allow any origin)

#### Step 4: Swagger Documentation
```csharp
services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Backoffice API", Version = "v1" });
    option.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "x-api-key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Authorization by x-api-key inside request's header",
        Scheme = "ApiKeyScheme"
    });
    // Add security requirement to all endpoints
});
```
**Outcome:** Swagger enabled on all endpoints

#### Step 5: Database Contexts
```csharp
var dbContextTimeout = Configuration.GetValue<int>("Custom:DbContextTimeout", 180);

services.AddDbContext<SportsbookContext>(o => 
    o.UseSqlServer(Configuration["ConnectionStrings:Sportsbook"], options =>
        options.CommandTimeout(dbContextTimeout)));

services.AddDbContext<TradingContext>(o => 
    o.UseSqlServer(Configuration["ConnectionStrings:Trading"], options =>
        options.CommandTimeout(dbContextTimeout)));

services.AddDbContextPool<BackofficeContext>(o => 
    o.UseSqlServer(Configuration["ConnectionStrings:BackOffice"], options =>
        options.CommandTimeout(dbContextTimeout)));

services.AddDbContextPool<TranslationContext>(o => 
    o.UseSqlServer(Configuration["ConnectionStrings:Translation"], options =>
        options.CommandTimeout(dbContextTimeout)));
```

**Sequence:**
1. Read `Custom:DbContextTimeout` from config (default: 180 seconds)
2. Register 4 DbContexts
3. Backoffice & Translation use `DbContextPool`
4. Sportsbook & Trading use regular `AddDbContext`

**Outcome:** 
- 2 contexts with pooling (better performance)
- 2 contexts without pooling
- All have 180-second timeout

#### Step 6: HTTP Client Registrations
```csharp
services.AddHttpClient<IAuthenticationRepository, AuthenticationRepository>(client =>
{
    client.BaseAddress = new Uri(Configuration["API:AuthenticationAPI:Uri"]);
    client.DefaultRequestHeaders.Add("x-api-key", 
        Configuration["API:AuthenticationAPI:Security:APIKey"]);
});

services.AddHttpClient<IRabbitMqRepository, RabbitMqRepository>();

services.AddHttpClient<IMaintenanceRepository, MaintenanceRepository>(client =>
{
    client.BaseAddress = new Uri(Configuration["API:Maintenance:Uri"]);
    client.DefaultRequestHeaders.Add("x-api-key", "API:Maintenance:Security:ApiKey");
});
```

**Outcome:** 3 HttpClient factories registered

#### Step 7: Repository Registration
```csharp
services.AddScoped<IBackofficeRepository, BackofficeRepository>();
services.AddScoped<IPlatformRepository, PlatformRepository>();
services.AddScoped<ISportsbookRepository, SportsbookRepository>();
services.AddScoped<ITradingRepository, TradingRepository>();
services.AddScoped<IBackofficeActivityLogRepository, BackofficeActivityLogRepository>();
services.AddSingleton<IMemCache, MemCache>();
```

**Outcome:** Repositories registered as scoped, cache as singleton

#### Step 8: Action Filters
```csharp
services.AddTransient<ApiKeyFilter>();
```

**Outcome:** ApiKeyFilter instantiated per use

#### Step 9: Health Checks
```csharp
services.AddHealthChecks();
```

**Outcome:** Health check service available

### 3. Startup.Configure (Middleware Pipeline)

#### Development Condition
```csharp
if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
```

#### Middleware Order
```
1. Swagger UI & Endpoint
   app.UseSwagger();
   app.UseSwaggerUI();

2. Routing
   app.UseRouting();

3. CORS
   app.UseCors();

4. Authorization
   app.UseAuthorization();

5. Endpoints
   app.UseEndpoints(endpoints =>
   {
       endpoints.MapControllers();
       endpoints.MapHealthChecks("/healthz");
   });
```

### 4. Application Startup Hooks

#### Before Requests Handled
- All DbContexts available (but not initialized)
- All repositories available (lazy-loaded)
- Serilog ready to log
- Telemetry ready to trace

#### Database Connection Validation
- **Timing:** First DbContext query
- **Behavior:** If connection fails, exception thrown
- **Result:** Application continues (no startup health check)

---

## Backoffice.Web Startup Sequence

### 1. Program.cs (Minimal APIs)

```csharp
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Services.BuildServiceProvider()
    .GetService<IConfiguration>();
```

⚠️ **Issue:** ServiceProvider built twice (wasteful)

### 2. Serilog Configuration
```csharp
builder.Host.UseSerilog((context, _, serilog) =>
{
    serilog.ReadFrom.Configuration(context.Configuration);
});
```

### 3. Telemetry Setup
```csharp
builder.Services.AddSingleton<Instrumentation>();
builder.Services.AddTelemetry(new TelemetrySettings(...));
```

### 4. Blazor & Session Setup
```csharp
builder.Services.AddHealthChecks();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(o => o.DetailedErrors = true);  // WARNING: Production risk
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
```

### 5. Custom Extensions
```csharp
builder.AddAuthorization()      // Extension method
    .AddHttpClients()           // Extension method
    .AddServices()              // Extension method
    .AddStates();               // Extension method
```

### 6. Services Registration
```csharp
builder.Services.AddScoped<ICommonService, CommonService>();
builder.Services.AddResponseCompression(...);
```

### 7. Build & Configure

```csharp
var app = builder.Build();

app.UseResponseCompression();
app.UseDeveloperExceptionPage();  // WARNING: Always enabled
app.UseSession();
app.MapHealthChecks("/healthz");
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub(o => { o.Transports = HttpTransportType.WebSockets; });
app.MapGet("/sso/metabase", ...);   // JWT provider for the embedded Metabase dashboard (see below)
app.MapFallbackToPage("/_Host");
app.Run();
```

### Metabase embedded dashboard SSO (`GET /sso/metabase`)

The dashboard pages (`/dashboard`, `/admin/dashboard`) render a `<metabase-dashboard>` web component from Metabase's
`embed.js`. On the first render the page calls `initMetabaseDashboard` (`wwwroot/js/metabaseInterop.v2.js`), which loads
`{Metabase:SiteUrl}/app/embed.js` with `jwtProviderUri = /sso/metabase`. Whenever the embed needs a Metabase session it calls
that endpoint with the browser's cookies:

1. The page resolves the user's client codes (Admin and Super Admin roles: every client from `GetClientList`; everyone else:
   their sportsbook clients) and caches them in `IMemCache` under `metabase-clients:{userId}` for 12 hours.
2. The endpoint reads `Session["UserToken"]` (401 when it is missing), takes `TokenName` and `UserId` from the BO token, and
   reads the client codes from that cache, falling back to the token's `ClientCodes` claim.
3. `MetabaseService.GetSsoToken` signs an HS256 JWT with `Metabase:SsoSecretKey`: `email` = `{username}@{Metabase:EmailDomain}`,
   `first_name` and `last_name` = the username, `clientcode` = the array of codes, `groups = ["Clients"]`, `iat` and an `exp`
   one hour later. The endpoint returns `{ "jwt": "..." }`.

Every BO user therefore becomes one Metabase account in the "Clients" group, created on the first login. Metabase bills
every account that has not been deactivated, used or not.

**Config (`Metabase` section):** `SiteUrl`, `SsoSecretKey` (placeholder in `appsettings.json`, real value injected from
the environment), `DashboardId`, `EmailDomain`. Metabase needs JWT authentication configured with the same shared secret.

### Metabase idle account cleanup (GM-987)

`MetabaseIdleUserCleanupService` (a `BackgroundService` in `Backoffice.Web`, registered in `AddServices`) deactivates the
Metabase accounts of the "Clients" group that have not logged in for a while, so the bill follows the accounts in use at
the same time instead of everyone who ever opened the dashboard. Metabase reactivates a deactivated account on its next
SSO login, which is the `/sso/metabase` flow above, so the user sees no interruption.

It is **off by default**. Settings, all under `Metabase:IdleCleanup` (environment variables use `__`, for example
`Metabase__IdleCleanup__Enabled`):

| Setting | Default | Meaning |
|---|---|---|
| `Enabled` | `false` | With `false` the job logs once that it is disabled and makes no Metabase calls |
| `IdleHours` | `3` | A member whose last login is older than this is deactivated (decimal, so STG can test in minutes; at most 8784) |
| `IntervalMinutes` | `15` | Time between rounds (0.001 to 1440) |
| `StartupDelaySeconds` | `30` | Pause before the first round (0 to 3600) |
| `GroupName` | `Clients` | The only group the job touches, matched by exact name |

It also needs `Metabase:SiteUrl` and `Metabase:ApiKey`. If either is missing, still a `${...}` placeholder, or a setting is out
of range or cannot be read, the job logs one error and does not run; the app keeps running.

**What Metabase has to provide:**
- A **JWT user provisioning** setting that is on (`jwt-user-provisioning-enabled`, `MB_JWT_USER_PROVISIONING_ENABLED`,
  default `true`). It is what makes the next JWT login of a deactivated user reactivate the account, per Metabase's docs
  ("users who log in via JWT will automatically get a Metabase account if they don't have one, or get their existing account
  reactivated"). With it off a deactivated user could not sign back in and this job would lock people out of the dashboard.
- An **API key in the Administrators group**, sent as `X-API-Key`. A key has the permissions of its group, and deactivating
  a user needs superuser rights.

Each round (`RunRoundAsync`): find the group, list its active members page by page, then deactivate every member whose
`last_login` is older than `IdleHours`. It never touches a superuser, an account with no `last_login`, or an account that is
not in the group. If Metabase does not report `group_ids`, `last_login` or `is_superuser` for an account (a sign that the key
is not an administrator key, or that the API differs from what was checked), the round fails with an error and changes
nothing: an account that cannot be classified is never guessed at. A failed deactivation is logged and the rest continue; a
failed round is logged and retried at the next interval (the whole round is wrapped, because an unhandled exception in a
hosted service stops the web host). Deactivations are sequential and rounds never overlap.

Log lines to search in Loki (`app="bo-web"`), all starting with `Metabase idle user cleanup`: `deactivated user {UserId}`
for each deactivation and `round finished: Group, Members, Deactivated, KeptActive, SkippedSuperuser, SkippedNoLogin,
Failed, DurationMs` once per round. The API key is never logged.

Things to know before enabling it:
- **Single replica only.** The job runs inside every `bo-web` replica, so a scaled-out deployment would run it once per
  replica. Deleting an already deactivated user is not an error in Metabase, so this is only wasted calls, but turn it on for
  one replica only. The other `bo-web` state (in-process cache, and DataProtection keys unless `DataProtection:KeysPath` points at shared storage) already assumes one instance.
- **DEV and STG share a Metabase.** `Metabase:SiteUrl` defaults to `https://metabase.stg.song88.com` and only the PRD argocd
  file overrides it, so `bo-web` in DEV and in STG both use the STG Metabase. Enable the job in one of them only, and expect it
  to deactivate accounts the other environment's users also use (they come back on their next login).
- `last_login` is when the account last logged in, not its last activity. A user who has been working for more than
  `IdleHours` is deactivated and comes back on the next `/sso/metabase` call. The member list is read once per round, so a user
  who logs in after it was read can still be deactivated that round; the next login brings them back the same way.
- The Metabase calls were checked against the Metabase 0.50 source for GM-987: `GET /api/user` returns
  `{data, total, limit, offset}` (an administrator's list has `id`, `email`, `last_login`, `is_superuser`, `is_active` and
  `group_ids`, and `limit` has no cap), `DELETE /api/user/{id}` needs a superuser and sets `is_active` to false, and
  `GET /api/permissions/group` lists the groups with `id` and `name`. Confirm them against the Metabase version you run.
- To switch it off, set `Metabase__IdleCleanup__Enabled` to `false` and restart the pod. Accounts deactivated in the meantime
  come back on their next login, or from the Deactivated tab of Metabase's People page.

---

## Backoffice.WebSocket.API Startup

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(...);
builder.Services.AddSingleton<Instrumentation>();
builder.Services.AddTelemetry(...);
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(...);

builder.Services.AddSignalR(options => 
    options.MaximumReceiveMessageSize = 5000000)
    .AddMessagePackProtocol(options =>
    {
        // MessagePack configuration
    });

builder.Services.AddResponseCompression(...);
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/healthz");
app.MapHub<BackofficeHub>("/bohub", options => {...});
```

---

## Backoffice.Config.Service Startup

```csharp
Host.CreateDefaultBuilder(args)
    .UseSerilog(...)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton<Instrumentation>();
        services.AddTelemetry(...);
        services.AddSingleton<IMessageBroker, MessageBroker>();
        services.AddSingleton<IBackofficeRepository, BackofficeRepository>();
        services.AddHostedService<Worker>();
    })
    .Build()
    .Run();
```

**Initialization Order:**
1. Telemetry
2. MessageBroker (singleton)
3. BackofficeRepository (singleton, creates DbContext manually)
4. Worker starts consuming messages

### Worker Startup (ExecuteAsync)
```csharp
protected override Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("Connecting to message broker");
    _messageBroker.Connect();
    _logger.LogInformation("Connected, processing incoming messages");
    _messageBroker.MessageReceived += MessageReceivedEvent;
    
    return Task.CompletedTask;  // Returns immediately
}
```

⚠️ **Issue:** Task completed before real work starts (event-driven)

---

## Backoffice.Utility Startup

Similar to Config.Service:
```csharp
Host.CreateDefaultBuilder(args)
    .UseSerilog(...)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton<Instrumentation>();
        services.AddTelemetry(...);
        services.AddSingleton<IMessageBroker, MessageBroker>();
        services.AddSingleton<IBackofficeRepository, BackofficeRepository>();
        services.AddHostedService<Worker>();
    })
    .Build()
    .Run();
```

---

## Configuration Loading Sequence

### 1. Default Configuration Sources
```
appsettings.json (base defaults)
    ↓
appsettings.{ASPNETCORE_ENVIRONMENT}.json (overrides)
    ↓
Environment Variables (highest priority)
    ↓
Command-line arguments (if provided)
```

### 2. Configuration Keys Retrieved During Startup

```
[API]
├─ AuthenticationAPI:Uri
├─ AuthenticationAPI:Security:APIKey
├─ AuthenticationAPI:JWT:Key
├─ AuthenticationAPI:JWT:Issuer
├─ PlatformAPI:{Region}:Uri
├─ PlatformAPI:{Region}:APIKey
├─ TradingAPI:{Region}:Uri
├─ TradingAPI:{Region}:APIKey
├─ SportsbookAPI:{Region}:Uri
└─ RabbitMqApi:{Region}:Uri

[ConnectionStrings]
├─ Backoffice
├─ Sportsbook
├─ Trading
└─ Translation

[Security]
└─ ApiKey

[Custom]
├─ MongoDB-Sportsbook
├─ DbContextTimeout
└─ MaintenanceConfigFile

[Telemetry]
├─ Environment
└─ Enabled

[Logging] / [Serilog]
└─ Serilog configuration
```

### 3. Secrets Management

⚠️ **Security Issue:** Database passwords in configuration file
```json
"ConnectionStrings": {
    "Backoffice": "Data Source=...; Password=<redacted>;..."
}
```

Should use:
- Azure Key Vault
- AWS Secrets Manager
- User Secrets (dev only)
- Environment variables

---

## Startup Health Checks

### What's Validated
- ✓ JSON syntax in appsettings.json
- ✓ Configuration can be read
- ✓ Services can be registered in DI container
- ✗ Database connectivity (not checked)
- ✗ External API availability (not checked)
- ✗ RabbitMQ connectivity (not checked)
- ✗ Configuration values are valid (not checked)

### Startup Failures
```
Would fail on:
├─ Invalid JSON in appsettings.json
├─ Missing required type registrations
├─ Circular DI dependencies
└─ Invalid connection strings (SQL parsing)

Would NOT fail on:
├─ Database unreachable (lazy initialization)
├─ External API unavailable
├─ RabbitMQ unavailable
└─ Configuration keys missing (silent defaults)
```

---

## Dependency Resolution During Startup

### Backoffice.API

```
1. Instrumentation created (singleton)
2. HttpClientFactory configured
3. DbContexts registered (not created yet)
4. Repositories registered (not created yet)
5. Filters registered
6. Middleware pipeline configured
7. First request triggers:
   ├─ DbContext instantiation
   ├─ Repository instantiation
   └─ Dependency graph resolution
```

### Backoffice.Config.Service

```
1. Instrumentation created (singleton)
2. MessageBroker created (singleton) 
3. BackofficeRepository created (singleton) with DbContext
4. Worker created (hosted service)
5. Worker.StartAsync() called
6. Worker.ExecuteAsync() runs event loop
7. First message triggers MessageReceivedEvent
```

---

## Runtime State After Startup

### Backoffice.API
```
Running:
├─ HTTP Server on configured port
├─ Swagger UI accessible
├─ Health check endpoint ready
├─ All services registered
└─ Listening for first request
```

### Backoffice.Web
```
Running:
├─ HTTP Server on configured port
├─ Blazor Server ready
├─ SignalR hub ready
├─ Session middleware ready
└─ Listening for connections
```

### Backoffice.WebSocket.API
```
Running:
├─ SignalR hub on /bohub
├─ WebSocket listeners active
├─ Message compression ready
└─ Listening for WebSocket connections
```

### Background Services
```
Running:
├─ Message broker connected
├─ Consumer groups active
├─ Event pump running
└─ Listening for RabbitMQ messages
```

---

## Startup Issues & Risks

### Critical
1. **No startup validation** - Errors only on first request
2. **Database credentials in appsettings** - Security risk
3. **Developer exception page always enabled** - Info leakage
4. **Exception handler commented out** - No production error handling

### High Priority
1. **Partial HttpClient configuration** - Some clients not configured
2. **ServiceProvider built twice** (Web) - Resource waste
3. **Static MongoDB client** - Thread safety concerns
4. **Manual DbContext creation** (Config.Service) - Not DI-friendly

### Medium Priority
1. **DetailedErrors in production** (Blazor) - Potential info leakage
2. **TODO comments in code** - Incomplete implementation
3. **Mixed serialization libraries** - Potential conflicts
4. **CORS allows any origin** - Security risk

### Low Priority
1. **Health checks basic** - No dependency validation
2. **Startup sequence could be optimized** - Parallel registration
3. **No startup metrics** - Hard to diagnose issues

