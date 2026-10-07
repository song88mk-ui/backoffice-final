# Dependency Graph

**Document Status:** Analysis Complete (No Code Changes Made)  
**Date:** June 18, 2026

## Project-Level Dependencies

```
Backoffice.Web
  ├─> Backoffice.Shared (direct reference)
  └─> (Runtime calls to Backoffice.API via HTTP)

Backoffice.API
  ├─> Backoffice.Shared (direct reference)
  └─> (Runtime calls to external APIs)

Backoffice.WebSocket.API
  ├─> Backoffice.Shared (direct reference)

Backoffice.Config.Service
  ├─> Backoffice.Shared (direct reference)

Backoffice.Utility
  ├─> Backoffice.Shared (direct reference)

Backoffice.WebSocket.Service
  ├─> Backoffice.Shared (direct reference)

Backoffice.Shared
  └─> (No dependencies on other projects)
```

## NuGet Package Dependencies

### Core Framework
```
Microsoft.AspNetCore.* (10.0.x)
├─ Microsoft.AspNetCore.Hosting
├─ Microsoft.AspNetCore.Http
├─ Microsoft.AspNetCore.Mvc
├─ Microsoft.AspNetCore.SignalR
├─ Microsoft.AspNetCore.Diagnostics.HealthChecks
└─ Microsoft.AspNetCore.ResponseCompression

Microsoft.EntityFrameworkCore (10.0.11)
├─ Microsoft.EntityFrameworkCore.SqlServer
├─ Microsoft.EntityFrameworkCore (core)
└─ (No auto-migrations visible)

Microsoft.Extensions.* (10.0.x)
├─ Microsoft.Extensions.Configuration
├─ Microsoft.Extensions.DependencyInjection
├─ Microsoft.Extensions.Hosting
├─ Microsoft.Extensions.Logging
└─ Microsoft.Extensions.Http
```

### Messaging
```
RabbitMQ.Client (5.2.0)
├─ Protocol implementation
├─ Connection management
├─ Channel operations
└─ Event handling
```

### Data Access
```
MongoDB.Driver (3.12.0)
├─ Connection pooling
├─ LINQ provider
├─ Collection operations
└─ Query execution

System.Runtime.Caching (7.0.0)
├─ MemoryCache
├─ CacheItemPolicy
└─ Cache expiration
```

### Logging & Telemetry
```
Serilog (4.3.0)
├─ Serilog.AspNetCore (10.0.0)
├─ Serilog.Formatting.Compact (3.0.0)
├─ Serilog.Settings.Configuration (10.0.0)
└─ Serilog.Sinks.Console (6.1.1)

OpenTelemetry (1.15.3)
├─ OpenTelemetry.Api
├─ OpenTelemetry.Instrumentation.AspNetCore
├─ OpenTelemetry.Exporter (implied)
└─ System.Diagnostics.Metrics (implied)
```

### Serialization
```
Newtonsoft.Json (13.0.1) [LEGACY - Mixed with System.Text.Json]
├─ Contract resolvers
├─ Settings
└─ Token readers

System.Text.Json (6.0.7) [PRIMARY]
├─ UTF8JsonWriter
├─ JsonSerializer
└─ JsonDocument
```

### UI / Web
```
Blazored.LocalStorage
├─ Local storage API wrapper
└─ Async storage access

Microsoft.AspNetCore.DataProtection
└─ Data protection for sensitive data

MessagePack (implied by WebSocket.API)
├─ Binary serialization
└─ ContractlessResolver
```

### Authentication
```
System.IdentityModel.Tokens.Jwt
├─ JWT parsing
├─ Token validation
└─ Claims extraction

Microsoft.IdentityModel.Tokens
└─ Token handler
```

### Utilities
```
UAParser (3.1.47)
├─ User agent parsing
└─ Browser/OS detection
```

### Custom Internal
```
Infra.Extensions.Telemetry (0.0.1-1)
├─ Custom telemetry setup
└─ OpenTelemetry helpers

OE.Shared.Data.Sportsbook
├─ Odds engine shared models
├─ Collection definitions
└─ SQL entities
```

## Dependency Injection Container

### Service Lifetimes

**Singleton (Instance per application)**
```
Instrumentation
  - ActivitySource
  - Counters
  - Meters
  - Telemetry configuration

IMemCache → MemCache
  - In-process memory cache
  - Shared across all requests

IMessageBroker → MessageBroker
  - RabbitMQ connection
  - Channel management
  - Static state

IHttpClientFactory (implicit)
  - Managed by ASP.NET Core
  - Connection pooling
```

**Scoped (Instance per HTTP request)**
```
DbContexts (all 4)
  - BackofficeContext
  - TradingContext
  - SportsbookContext
  - TranslationContext

Repositories (IXxxRepository)
  - IBackofficeRepository → BackofficeRepository
  - ITradingRepository → TradingRepository
  - IPlatformRepository → PlatformRepository
  - ISportsbookRepository → SportsbookRepository
  - IAuthenticationRepository → AuthenticationRepository
  - IRabbitMqRepository → RabbitMqRepository

Services (IXxxService)
  - ICommonService → CommonService
  - ITradingService → TradingService

Action Filters
  - ApiKeyFilter
  - AuthorizeAttribute

AuthenticationStateProvider
  - RevalidatingIdentityAuthenticationStateProvider
```

**Transient (New instance each time)**
```
HttpClient (via AddHttpClient registrations)
  - IAuthenticationRepository (configured)
  - IMaintenanceRepository (configured)
  - IWebPropsRepository (configured)
  - IRabbitMqRepository (configured)
  - IPlatformRepository (via IBackofficeActivityLogRepository)
  - ISportsbookRepository (via GetHttpClient)

Controllers
  - Implicitly registered
  - Instantiated per request
```

## Constructor Injection Chains

### Backoffice.API

**BackofficeRepository** constructor:
```
BackofficeRepository(
  BackofficeContext backofficeContext,           ← Scoped DbContext
  TranslationContext translationContext,         ← Scoped DbContext
  IPlatformRepository platformRepository,        ← Scoped → needs IPlatformRepository
  ISportsbookRepository sportsbookRepository,    ← Scoped → needs ISportsbookRepository
  ITradingRepository tradingRepository,          ← Scoped → needs ITradingRepository
  IMemCache memCache,                            ← Singleton
  IRabbitMqRepository rabbitMqRepository         ← Transient HttpClient
)
```

**Dependency Resolution:**
```
BackofficeRepository
  ├─ BackofficeContext (auto-injected)
  ├─ TranslationContext (auto-injected)
  ├─ IPlatformRepository
  │   └─ PlatformRepository
  │       ├─ IConfiguration
  │       ├─ IBackofficeActivityLogRepository
  │       │   └─ BackofficeActivityLogRepository
  │       │       └─ IRabbitMqRepository → RabbitMqRepository (HttpClient)
  │       └─ IRabbitMqRepository → RabbitMqRepository
  │
  ├─ ISportsbookRepository
  │   └─ SportsbookRepository
  │       ├─ SportsbookContext
  │       ├─ IConfiguration
  │       ├─ IPlatformRepository
  │       ├─ IBackofficeActivityLogRepository
  │       ├─ IRabbitMqRepository
  │       └─ IMemCache
  │
  ├─ ITradingRepository
  │   └─ TradingRepository (constructor)
  │       ├─ TradingContext
  │       ├─ IConfiguration
  │       ├─ IBackofficeActivityLogRepository
  │       ├─ IMemCache
  │       └─ ILogger<TradingRepository>
  │
  ├─ IMemCache → MemCache (Singleton)
  │
  └─ IRabbitMqRepository → RabbitMqRepository
      └─ HttpClient (injected)
```

### Backoffice.Web

**CommonService** constructor:
```
CommonService(
  IConfiguration configuration
)
```

**Program.cs Extensions:**
```
AddAuthorization()
  └─ Registers authorization services

AddHttpClients()
  └─ Registers all HttpClient factories
      ├─ IBackofficeAPIClient
      ├─ IPlatformAPIClient
      ├─ ISportsbookAPIClient
      └─ etc.

AddServices()
  └─ Registers business services
      ├─ ICommonService → CommonService
      └─ ITradingService → TradingService

AddStates()
  └─ Registers Blazor state objects
```

### Backoffice.Config.Service

**BackofficeRepository** (Config Service version):
```
BackofficeRepository(
  IConfiguration configuration,                  ← Runtime config
  ILogger<BackofficeRepository> logger           ← Logging
)
```

**Direct DbContext Creation:**
```
var optionsBuilder = new DbContextOptionsBuilder<BackofficeContext>()
  .UseSqlServer(_configuration["ConnectionStrings:Backoffice"]);
_backofficeContext = new BackofficeContext(optionsBuilder.Options);
```

⚠️ **Issue:** DbContext created manually, not via DI

---

## HTTP Client Configuration

### Backoffice.API Startup

```csharp
services.AddHttpClient<IAuthenticationRepository, AuthenticationRepository>(client =>
{
    client.BaseAddress = new Uri(Configuration["API:AuthenticationAPI:Uri"]);
    client.DefaultRequestHeaders.Add("x-api-key", 
        Configuration["API:AuthenticationAPI:Security:APIKey"]);
});

services.AddHttpClient<IMaintenanceRepository, MaintenanceRepository>(client =>
{
    client.BaseAddress = new Uri(Configuration["API:Maintenance:Uri"]);
    client.DefaultRequestHeaders.Add("x-api-key", "API:Maintenance:Security:ApiKey");
});

services.AddHttpClient<IWebPropsRepository, WebPropsRepository>(client =>
{
    client.BaseAddress = new Uri(Configuration["API:WebProps:Uri"]);
    client.DefaultRequestHeaders.Add("x-api-key", Configuration["API:WebProps:Security:ApiKey"]);
});

services.AddHttpClient<IRabbitMqRepository, RabbitMqRepository>();
```

**Issues:**
- Incomplete configuration for some clients
- RabbitMqRepository creates new HttpClient anyway
- PlatformRepository and SportsbookRepository don't use factory

---

## Database Context Dependencies

```
DbContextOptions<T>
  ├─ Backoffice.API/Startup.cs
  │  ├─ AddDbContext<BackofficeContext>() [DbContextPool]
  │  ├─ AddDbContext<TradingContext>()
  │  ├─ AddDbContext<SportsbookContext>()
  │  └─ AddDbContext<TranslationContext>() [DbContextPool]
  │
  └─ Backoffice.Config.Service/Repository.cs
     └─ Manual instantiation [NOT recommended]
```

**Connection String Resolution:**
```
appsettings.json → IConfiguration
  ├─ ConnectionStrings:Backoffice
  ├─ ConnectionStrings:Trading
  ├─ ConnectionStrings:Sportsbook
  ├─ ConnectionStrings:Translation
  └─ Custom:MongoDB-Sportsbook

Custom:DbContextTimeout → 180 seconds
```

---

## Data Flow Dependencies

```
HTTP Request
  ↓
ApiKeyFilter (validates x-api-key)
  ↓
Controller Action
  ↓
IBackofficeRepository
  ├─→ BackofficeContext (SQL)
  ├─→ TranslationContext (SQL)
  ├─→ IPlatformRepository
  │   └─→ HttpClient → Platform API
  ├─→ ISportsbookRepository
  │   ├─→ SportsbookContext (SQL)
  │   ├─→ MongoDB (static collection)
  │   └─→ HttpClient → Sportsbook API
  ├─→ ITradingRepository
  │   └─→ TradingContext (SQL)
  ├─→ IMemCache
  │   └─→ System.Runtime.Caching.MemoryCache
  └─→ IRabbitMqRepository
      └─→ HttpClient → RabbitMQ Management API
```

---

## Message Flow Dependencies

```
RabbitMQ Topic Exchange
  ├─→ Config.Service consumes
  │   └─→ IBackofficeRepository
  │       └─→ BackofficeContext (SQL)
  │
  ├─→ Utility consumes
  │   └─→ IBackofficeRepository
  │       └─→ BackofficeContext (SQL)
  │
  └─→ WebSocket.Service consumes
      └─→ SignalR Hub
          └─→ Connected WebSocket Clients
```

---

## External API Dependencies

```
Backoffice.API
  ├─→ Authentication API
  │   ├─ Login endpoint
  │   └─ Token validation
  │
  ├─→ Platform API
  │   ├─ Client queries
  │   └─ Configuration updates
  │
  ├─→ Sportsbook API
  │   ├─ Fixture data
  │   └─ Market templates
  │
  ├─→ Trading API
  │   ├─ Odds updates
  │   └─ Settlement data
  │
  ├─→ Maintenance API
  │   └─ System operations
  │
  └─→ webprops-api (sibling repo, ../webprops-api/)
      └─ Per-client feature flags (GET/POST /api/v1/config/{clientId})
```

---

## External System Dependencies

```
Backoffice System
  ├─→ SQL Server (4 databases)
  │   ├─ Backoffice database
  │   ├─ Trading database
  │   ├─ Sportsbook database
  │   └─ Translation database
  │
  ├─→ MongoDB
  │   └─ Sportsbook collection cache
  │
  ├─→ RabbitMQ
  │   ├─ Message publishing
  │   └─ Consumer groups
  │
  ├─→ Redis (implied but not visible)
  │   └─ Session storage? (not confirmed)
  │
  └─→ External APIs
      ├─ Platform
      ├─ Auth
      ├─ Sportsbook
      └─ Trading
```

---

## Circular Dependencies

### Potential Issues

**PlatformRepository → IBackofficeActivityLogRepository**
```
PlatformRepository needs IBackofficeActivityLogRepository
    ↓
BackofficeActivityLogRepository needs IRabbitMqRepository
    ↓
RabbitMqRepository is transient HttpClient-based
    ✓ No circular dependency
```

**BackofficeRepository → IPlatformRepository → IBackofficeActivityLogRepository**
```
BackofficeRepository needs IPlatformRepository
    ↓
PlatformRepository needs IBackofficeActivityLogRepository
    ↓
BackofficeActivityLogRepository needs IRabbitMqRepository
    ✓ No circular dependency
```

### Analysis
- **No direct circular dependencies detected**
- However, complex inter-repository dependencies
- Could benefit from refactoring into service layer

---

## Dependency Resolution Performance

### Singleton Resolution (cached)
- Instrumentation: ✓ Fast
- MemCache: ✓ Fast  
- MessageBroker: ✓ Fast

### Scoped Resolution (per request)
- DbContexts (4): Medium (pool overhead)
- Repositories (complex graph): Slow
  - BackofficeRepository creates several sub-repositories
  - PlatformRepository creates IBackofficeActivityLogRepository
  - SportsbookRepository creates MongoDB connections

### Optimization Opportunities
1. Lazy<T> for expensive dependencies
2. Parallel dependency resolution
3. Object pooling for frequent instantiations

---

## Summary Statistics

| Metric | Count |
|--------|-------|
| Projects | 7 |
| NuGet Dependencies | 20+ |
| Registered Services | 15+ |
| DbContexts | 4 |
| HTTP Clients | 6+ |
| Repositories | 8 |
| External APIs | 5+ |

