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
app.MapGet("/sso/metabase", ...);   // Metabase full-app embedding SSO redirect (see below)
app.MapFallbackToPage("/_Host");
app.Run();
```

### Metabase full-app embedding SSO (`GET /sso/metabase`)

The dashboard pages (`/dashboard`, `/admin/dashboard`) embed Metabase via an `<iframe src="/sso/metabase">`.
Because the BO access token lives in the cookie-backed server session (`HttpContext.Session["UserToken"]`),
the same-origin iframe navigation carries the session cookie, so the endpoint can identify the user:

1. Read `Session["UserToken"]`; if absent → redirect to `/login`.
2. Seed `UserState` from the token and resolve the user's client codes via `MetabaseService.ResolveClientCodesAsync()`
   (admins/super admins → `GetClientList`; others → their `GetClientsV2` sportsbook clients — the same queries the
   dashboard used before).
3. Sign a Metabase JWT (`MetabaseService.BuildFullAppEmbedSsoUrl`) with claims `email`, `first_name`, `last_name`
   (all synthesized from the username as `{username}@clients.song88.com`), `clientcode` (CSV of the resolved codes),
   `groups = ["Clients"]`, `iat`, `exp`, signed HS256 with `Metabase:FullAppEmbedKey`.
4. `302` redirect to `{Metabase:SiteUrl}/auth/sso?jwt=…&return_to=/dashboard/{DashboardId}?client=…`.

**Config (`Metabase` section):** `SiteUrl`, `DashboardId`, `TokenLifetimeMinutes`, and `FullAppEmbedKey`
(the JWT shared secret — placeholder in `appsettings.json`; the real value is supplied via git-ignored config /
secrets, 1Password `metabase:stg:fullappembed:key`). Metabase must have interactive embedding enabled, JWT auth
configured with the same shared secret, and the BO origin allowed as an embedding origin.

> Replaces the previous static `email`-only `GET /api/metabase/sso-token` + embed.js `<metabase-dashboard>` flow,
> which authenticated every BO user as one shared Metabase identity and filtered clients only via hidden URL params.

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

