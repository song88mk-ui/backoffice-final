# Backoffice Solution - Architecture Analysis Report

**Date:** June 18, 2026  
**Target Framework:** .NET Core 3.1  
**Analysis Scope:** Complete solution architecture, patterns, dependencies, and design decisions

---

## Executive Summary

The Backoffice solution is a **multi-tier, event-driven microservices application** designed to manage sports betting operations. The architecture implements enterprise-grade patterns including repository-based data access, dependency injection, message-driven communication, distributed tracing, and multi-region support. The system is built on .NET Core 3.1 with SQL Server databases and RabbitMQ for asynchronous messaging.

---

## 1. Overall Architecture

### Architecture Type
- **Multi-tier Architecture** with microservices characteristics
- **Event-Driven Architecture** using RabbitMQ message broker
- **API-First Design** with REST and WebSocket support
- **Distributed Tracing** via OpenTelemetry

### Core Philosophy
- Separation of concerns through layered architecture
- Scalability through background services and async messaging
- Real-time updates via WebSocket connections
- Region-aware multi-tenancy (Hong Kong, South Africa)
- Security through API keys, JWT, and custom authorization filters

---

## 2. Major Projects Overview

### 2.1 Backoffice.API
**Role:** Primary REST API gateway for backoffice operations  
**Framework:** ASP.NET Core 3.1  
**Port:** Configurable (default dev: 5000+)

**Key Responsibilities:**
- HTTP REST endpoint for all backoffice operations
- Authentication and authorization orchestration
- Data access through multiple repositories
- Activity logging and audit trails
- Configuration management for trading operations
- Client and market settings management
- Bet settlement and manual operations
- Cache invalidation messaging

**Key Dependencies:**
- Backoffice.Shared (shared models, entities, utilities)
- Entity Framework Core
- RabbitMQ Client

**Main Controllers:**
- **AuthenticationController** (`api/authentication`)
  - Login endpoint
  - Token refresh
  - Authorization delegation to Auth API
  
- **BackofficeController** (`api/backoffice`)
  - Activity logging
  - Core backoffice operations
  - Configuration updates
  
- **TradingController** (`api/trading`)
  - Market mapping management
  - Provider mappings
  - Trading configuration
  - Settlement history export
  
- **SettingsController** (`sportsbook/settings`)
  - Client configuration (margins, market settings)
  - Client-specific settings
  - Betting restrictions
  
- **ClientController** (`sportsbook/client`)
  - Client information retrieval
  - Client listing and filtering

---

### 2.2 Backoffice.Web
**Role:** Server-side Blazor web application  
**Framework:** ASP.NET Core 3.1 + Blazor Server  
**Rendering:** Server-side with WebSocket real-time updates

**Key Responsibilities:**
- Web UI for backoffice management
- Real-time data visualization
- Client configuration interface
- Trading settings management
- Activity log viewing
- User session management
- Local caching with Blazored.LocalStorage

**Key Features:**
- Blazor components for UI
- SignalR integration for real-time updates
- Health checks endpoint
- Metabase SSO integration

**Key Dependencies:**
- Blazored.LocalStorage
- Custom CommonService for business logic
- Trading service abstractions
- HTTP clients for API calls

---

### 2.3 Backoffice.WebSocket.API
**Role:** Real-time bidirectional communication gateway  
**Framework:** ASP.NET Core 3.1 + SignalR with MessagePack  
**Transport:** WebSockets (primary), Server-Sent Events (fallback), Long Polling (fallback)

**Key Responsibilities:**
- WebSocket hub for real-time client connections
- Message compression and serialization
- Broadcasting updates to connected clients
- Health checks for connection monitoring

**Key Hub:**
- **BackofficeHub** (`/bohub`)
  - Real-time message transmission
  - Large buffer support (5MB max message size)
  - Multiple transport protocol support

**Key Features:**
- MessagePack protocol for efficient serialization
- Automatic fallback transport negotiation
- Circuit retention for disconnected clients (3 minutes)

---

### 2.4 Backoffice.WebSocket.Service
**Role:** Message processing and distribution service  
**Framework:** ASP.NET Core Worker Service  
**Execution Model:** Background service with RabbitMQ consumer

**Key Responsibilities:**
- Consume outgoing messages from RabbitMQ
- Process different message types
- Route messages to WebSocket hub
- Activity log aggregation
- Settlement history processing
- Cache invalidation
- Configuration change distribution

**Message Types Processed:**
- `LogActivity` - Single activity log entries
- `LogActivityList` - Batch activity logs
- `ExportMessageSettlementHistory` - Manual message settlement
- `ExportManualBetSettlementHistory` - Manual bet settlement
- `ExportMarketSettlementHistory` - Market settlement export
- `UpdateClientMarketConfig` - Client market configuration updates
- `UpdateClientLeagueConfig` - Client league configuration updates
- `DeleteCache` - Cache invalidation requests
- `BulkUpdateConfig` - Bulk configuration updates
- `CopyClientSettings` - Client settings duplication
- `ConfigExport` - Configuration export operations

**Key Features:**
- OpenTelemetry activity tracking for distributed tracing
- Correlation ID propagation for request tracing
- Message acknowledgment pattern
- Error handling with retry logic

---

### 2.5 Backoffice.Config.Service
**Role:** Configuration management background service  
**Framework:** ASP.NET Core Worker Service  
**Execution Model:** Background service with RabbitMQ consumer

**Key Responsibilities:**
- Monitor incoming configuration messages
- Update client margins and settings
- Delete and manage configuration templates
- Database persistence of configuration changes

**Message Types Processed:**
- `UpdateClientMargin` - Client margin updates (long value)
- `DeleteConfigTemplate` - ConfigTemplate entity deletion

**Key Features:**
- Telemetry instrumentation
- Single repository pattern for database access
- Message broker for event notification

---

### 2.6 Backoffice.Utility
**Role:** General-purpose background service for asynchronous operations  
**Framework:** ASP.NET Core Worker Service  
**Execution Model:** Background service with RabbitMQ consumer

**Key Responsibilities:**
- Process settlement history exports (messages, manual bets, market settlements)
- Export configuration templates
- Generate reports
- Perform bulk configuration operations
- Handle cache invalidation

**Message Types Processed:**
- `LogActivity` / `LogActivityList` - Activity log processing
- `ExportMessageSettlementHistory` - Export message settlement details
- `ExportManualBetSettlementHistory` - Export manual bet settlements
- `ExportMarketSettlementHistory` - Export market settlements
- `ExportOeConfigTemplate` - Export Odds Engine configuration
- `BulkUpdateConfig` - Bulk configuration updates
- `CopyClientSettings` - Copy client settings
- `ExportDeletedTemplate` - Export deleted templates

**Key Features:**
- Full OpenTelemetry integration
- Baggage and activity context propagation
- RabbitMQ routing key and correlation tracking
- Comprehensive error logging

---

### 2.7 Backoffice.Shared
**Role:** Shared library with entities, models, and utilities  
**Framework:** .NET Standard Class Library  
**Distribution:** Consumed by all other projects

**Responsibility Breakdown:**

#### 2.7.1 Data Access Layer (`Backoffice.Shared/Data/`)

**Database Contexts:**

1. **BackofficeContext** (SQL Server)
   - Tables: ActivityLog, ClientInformation, ClientBetSetting, ConfigTemplate, ConfigTemplateAssigned, ConfigTemplateAssignedMapping, ConfigTemplateMargin
   - Purpose: Core backoffice operational data
   - Connection String: `ConnectionStrings:BackOffice`

2. **TradingContext** (SQL Server)
   - Purpose: Trading-related configurations and mappings
   - Entities: Sport mappings, League mappings, Market mappings, Selection mappings
   - Connection String: `ConnectionStrings:Trading`

3. **SportsbookContext** (SQL Server)
   - Purpose: Sportsbook data from odds engine
   - Collections: Fixtures, Markets, Selections, Sports, Leagues, Segments
   - Connection String: `ConnectionStrings:Sportsbook`
   - **Note:** Also syncs with MongoDB: `mongodb://10.49.100.55:27017/sportsbook`

4. **TranslationContext** (SQL Server)
   - Purpose: Multi-language translation data
   - Connection String: `ConnectionStrings:Translation`

#### 2.7.2 Models (`Backoffice.Shared/Models/`)

**Core Models:**
- `ActivityLogModel` - Activity audit trail
- `BetDetails` - Detailed bet information
- `BetSelectionDetails` - Individual selection in a bet
- `ClientInformationModel` - Client/operator metadata
- `ClientMarginModel` - Client margin settings
- `ClientMarketConfigModel` - Client market-specific configuration
- `ClientTopSetup` / `ClientTopFixtureSetup` - Top event configurations
- `ConfigModel` - Configuration with prematch and live variants
- `ConfigSettingsModel` - Market and league configuration settings

**Request/Response Models:**
- Located in `Models/Requests/` and `Models/Response/` subdirectories
- Examples: `AddOrUpdateBetConfigRequest`, `ClientListResponse`, `ManualBetDetailsModel`

**Base Response Model:**
```csharp
public class BaseResponse<T>
{
    public StatusCode StatusCode { get; set; }
    public string Message { get; set; }
    public T Result { get; set; }
}
```

#### 2.7.3 Enumerations (`Backoffice.Shared/Enums/`)

**Key Enums:**
- `StatusCode` - Response status (Success, Error, Validation_Error, Not_Found, etc.)
- `Activity` - Activity types for audit logging
- `BetStatus` - Bet states (Pending, Settled, Cancelled, etc.)
- `AlertStatus` - Alert conditions
- `Access` - Permission levels
- `BetSettingCategory` - Bet configuration categories
- `ConfigTemplateNavMenu` - UI navigation mapping
- `Currency` - Supported currencies (BTC, ETH, USD, EUR, etc.)
- `ClientSorting` - Client list sort options
- `ConfigState` - Configuration state tracking
- `AnnouncementAlertType` - Alert type classification

#### 2.7.4 Outgoing Engine Messages (`Backoffice.Shared/OutgoingEngineMessages/`)

**Purpose:** Messages published TO odds engine and other services

**Message Classes:**
- `OnActivityLogMessage` / `OnActivityLogListMessage` - Activity audit events
- `OnUpdateClientMarketConfigMessage` - Market configuration broadcasts
- `OnUpdateClientLeagueConfigMessage` - League configuration broadcasts
- `OnBulkUpdateConfigMessage` - Batch configuration updates
- `OnCopyClientSettingsMessage` - Settings replication
- `OnDeleteCacheMessage` - Cache invalidation
- `OnConfigMyTemplateExportMessage` - Template export
- `OnGetOeConfigTemplateMessage` - Configuration retrieval
- `OnGetMarketSettlementHistoryMessage` - Settlement export
- `OnGetManualBetSettlementHistoryMessage` - Manual settlement export

#### 2.7.5 Incoming SB Engine Messages (`Backoffice.Shared/IncomingSbEngineMessages/`)

**Purpose:** Messages RECEIVED FROM sportsbook engine

**Message Classes:**
- `IncomingOddsChangeMessage` - Odds updates with market/selection details
- `IncomingBetStopGlobalMessage` - Global bet stop signals
- `IncomingBetStopFixtureMessage` - Fixture-level bet stops
- `IncomingMarketMessage` - Market status changes
- `IncomingSelectionMessage` - Selection/outcome changes
- `IncomingPeriodScore` - Live score updates

#### 2.7.6 WebSocket Messages (`Backoffice.Shared/OutgoingWebSocketMessages/`)

**Purpose:** Real-time messages sent TO clients via WebSocket

**Message Types:**
- Price/odds updates
- Status changes
- Configuration updates
- Alert notifications

#### 2.7.7 Utilities (`Backoffice.Shared/Utilities/`)

**Key Utility Classes:**

1. **IMemCache / MemCache**
   - Implementation: `System.Runtime.Caching.MemoryCache`
   - Methods:
     - `AddOrUpdateCache<T>(key, object, timeout)` - Add with TTL
     - `GetCache<T>(key)` - Retrieve from cache
     - `GetCache<T>(key, repositoryCall, expiration)` - Lazy load pattern
     - `DeleteCache(key)` - Invalidate entry
   - Usage: Caching of expensive queries, configuration data

2. **IMessageBroker / MessageBroker**
   - Purpose: RabbitMQ wrapper for publish/subscribe
   - Features:
     - Multi-region connection support (HK, SA)
     - Exchange and queue declaration
     - Message publish with type and correlation tracking
     - Consumer setup with prefetch counts
     - SSL/TLS support with conditional enabling
     - Network recovery with 5-second intervals
     - OpenTelemetry activity propagation
   - Configuration:
     - Host, Port, Username, Password, VirtualHost
     - Exchange name, Queue name, Routing key
     - Consumer tags and prefetch settings
     - Exclusive/Priority queue options

3. **JwtParser**
   - Purpose: JWT token parsing and validation
   - Extracts claims from bearer tokens

4. **SportHelper** (static utility)
   - Purpose: Sports data transformation
   - Domain-specific helper methods

5. **SportsLiveTimeline**
   - Purpose: Live event timeline tracking
   - Timeline state management for live matches

6. **MockData**
   - Purpose: Test data generation

#### 2.7.8 Extensions (`Backoffice.Shared/Extensions/`)

**Purpose:** Extension methods for types and services

#### 2.7.9 External APIs (`Backoffice.Shared/ExternalApis/`)

**Purpose:** Client SDKs for third-party services

#### 2.7.10 Booking Engine Messages

**OutgoingEngineBookmakerMessages** - Messages to betting engine  
**IncomingSbEngineMessages** - Messages from SB engine (covered above)

---

## 3. Shared Libraries

### 3.1 Internal Shared Library (Backoffice.Shared)
- **Type:** Direct project reference
- **Distribution:** Compiled into dependent projects
- **Contains:** Models, DBContexts, Utilities, Enums, Messages

### 3.2 External Dependencies
- **Infra.Extensions.Telemetry** (v0.0.1-1)
  - Custom telemetry configuration
  - OpenTelemetry integration helpers
  
- **OE.Shared.Data.Sportsbook** (referenced in Startup)
  - Shared sportsbook entities
  - Odds engine data models

---

## 4. Repository Patterns

### 4.1 Repository Interface Pattern

All repositories follow the interface-based pattern:
```csharp
public interface IXxxRepository
{
    Task<BaseResponse<T>> GetXxx(...);
    Task<BaseResponse<bool>> UpdateXxx(...);
    Task<BaseResponse<bool>> DeleteXxx(...);
    // ... domain-specific methods
}

public class XxxRepository : IXxxRepository
{
    // Implementation
}
```

### 4.2 Core Repositories in Backoffice.API

#### IBackofficeRepository
**Implementation:** BackofficeRepository

**Responsibilities:**
- Activity log management
- Client information queries
- Market and league configuration
- Bet configuration and settlement
- Template management (create, read, update, delete)
- Client margin calculations
- Database transaction management

**Transaction Strategy:**
- Uses `TransactionScope` with `ReadCommitted` isolation level
- Async-aware transaction flow (`TransactionScopeAsyncFlowOption.Enabled`)
- Automatic rollback on exceptions

**Multi-Repository Composition:**
```csharp
BackofficeRepository depends on:
  - IPlatformRepository
  - ISportsbookRepository
  - ITradingRepository
  - IRabbitMqRepository (for messaging)
  - IMemCache (for caching)
```

#### ITradingRepository
**Implementation:** TradingRepository

**Responsibilities:**
- Market mapping (sport, league, fixture, market, selection)
- Provider mapping management
- Trading configuration
- Settlement history export
- Caching of trading data

#### IPlatformRepository
**Implementation:** PlatformRepository (HttpClient-based)

**Responsibilities:**
- Communication with Platform API
- Delegation of platform-specific operations
- Multi-region request routing (HK, SA)

#### ISportsbookRepository
**Implementation:** SportsbookRepository (HttpClient-based)

**Responsibilities:**
- Communication with Sportsbook service
- Client data retrieval
- Fixture and market lookups

#### IAuthenticationRepository
**Implementation:** AuthenticationRepository (HttpClient-based)

**Responsibilities:**
- Communication with Authentication API
- Login endpoint delegation
- Token validation
- Bearer token authentication

**Configuration:**
- Base address from `API:AuthenticationAPI:Uri`
- API key from `API:AuthenticationAPI:Security:APIKey`
- JWT settings from `API:AuthenticationAPI:JWT`

#### IMaintenanceRepository
**Implementation:** MaintenanceRepository (HttpClient-based)

**Responsibilities:**
- Communication with Maintenance API
- System maintenance operations

#### IRabbitMqRepository
**Implementation:** RabbitMqRepository (HttpClient-based)

**Responsibilities:**
- REST API wrapper around RabbitMQ management plugin
- Message publishing via HTTP
- Queue status checks

### 4.3 Background Service Repositories

#### Backoffice.Config.Service
**Repository:** IBackofficeRepository / BackofficeRepository
- Updates margins
- Deletes templates

#### Backoffice.Utility
**Repository:** IBackofficeRepository / BackofficeRepository
- Processes activity logs
- Exports settlement histories
- Manages configuration exports

#### Backoffice.WebSocket.Service
**No repositories** - routes messages to WebSocket hub

#### Backoffice.Web
**Repositories:** Injected via HTTP clients
- Uses `CommonService` abstraction
- Makes HTTP calls to Backoffice.API

---

## 5. Service Patterns

### 5.1 Blazor Web Services

**ICommonService / CommonService** (Backoffice.Web)
- Business logic orchestration
- Data transformation
- Claim/permission processing
- HTML generation for UI

**ITradingService / TradingService** (Backoffice.Web)
- Trading-specific business logic
- Market and league operations

### 5.2 WebSocket Service

**ISportsbookService / SportsbookService** (Backoffice.WebSocket.Service)
- Sportsbook-specific operations
- Real-time data formatting

### 5.3 Service Registration Pattern

**Example from Backoffice.API Startup:**
```csharp
services.AddScoped<IBackofficeRepository, BackofficeRepository>();
services.AddScoped<IPlatformRepository, PlatformRepository>();
services.AddScoped<ISportsbookRepository, SportsbookRepository>();
services.AddScoped<ITradingRepository, TradingRepository>();
services.AddHttpClient<IAuthenticationRepository, AuthenticationRepository>();
services.AddHttpClient<IMaintenanceRepository, MaintenanceRepository>();
services.AddHttpClient<IRabbitMqRepository, RabbitMqRepository>();
```

---

## 6. Dependency Injection Patterns

### 6.1 DI Container Setup

All projects use Microsoft.Extensions.DependencyInjection:

**Lifetime Patterns:**
- **Singleton:** Instrumentation, TelemetrySettings, IMemCache, IMessageBroker
- **Scoped:** Repositories, Services, DbContext
- **Transient:** Filters, Transient HTTP clients

### 6.2 Constructor Injection Pattern

```csharp
public class BackofficeRepository : IBackofficeRepository
{
    private readonly BackofficeContext _context;
    private readonly IPlatformRepository _platformRepository;
    private readonly IMemCache _memCache;
    
    public BackofficeRepository(
        BackofficeContext context,
        IPlatformRepository platformRepository,
        IMemCache memCache)
    {
        _context = context;
        _platformRepository = platformRepository;
        _memCache = memCache;
    }
}
```

### 6.3 HttpClient Factory Pattern

```csharp
services.AddHttpClient<IAuthenticationRepository, AuthenticationRepository>(client =>
{
    client.BaseAddress = new Uri(Configuration["API:AuthenticationAPI:Uri"]);
    client.DefaultRequestHeaders.Add("x-api-key", Configuration["...APIKey"]);
});
```

### 6.4 Configuration Binding

```csharp
services.AddDbContextPool<BackofficeContext>(o => 
    o.UseSqlServer(Configuration["ConnectionStrings:BackOffice"]));

_credential = configuration.GetSection("MessageBroker:HK")
    .Get<MessageBrokerCredentialModel>();
```

---

## 7. Caching Strategy

### 7.1 Caching Implementation

**Provider:** System.Runtime.Caching.MemoryCache (in-process memory)

**Interface:**
```csharp
public interface IMemCache
{
    void AddOrUpdateCache<T>(string key, T cacheObject, int cacheTimeout);
    void AddOrUpdateCache<T>(string key, T cacheObject, DateTimeOffset date);
    T GetCache<T>(string key, Func<T> repositoryCall, int expiration) where T : class;
    T GetCache<T>(string key) where T : class;
    void DeleteCache(string key);
}
```

### 7.2 Caching Patterns

**Pattern 1: Lazy Loading**
```csharp
var result = _memCache.GetCache<ClientList>(
    cacheKey,
    () => _repository.GetClients(),
    expirationHours: 1
);
```

**Pattern 2: Direct Retrieval**
```csharp
var cached = _memCache.GetCache<ClientList>(cacheKey);
if (cached == null) { /* query database */ }
```

**Pattern 3: Explicit Set**
```csharp
_memCache.AddOrUpdateCache(key, data, cacheTimeoutHours: 2);
```

### 7.3 Cache Invalidation

**Mechanism:** RabbitMQ message `OnDeleteCacheMessage`

**Flow:**
1. API receives configuration update
2. Publishes `OnDeleteCacheMessage` to RabbitMQ
3. Utility service consumes message
4. Broadcasts via WebSocket or calls API to clear cache

**Cached Data Examples:**
- Client configurations
- Market mappings
- League configurations
- Trading templates
- Sport hierarchies

### 7.4 Cache Expiration Strategy

- **Configuration data:** 1-2 hours
- **Client information:** 30 minutes
- **Lookup tables:** 24 hours (with explicit invalidation)
- **Temporary data:** 5 minutes

---

## 8. MongoDB Usage

### 8.1 MongoDB Integration Points

**Sportsbook MongoDB:**
- **Connection String:** `mongodb://10.49.100.55:27017/sportsbook?retryWrites=true&w=0`
- **Purpose:** Redundant storage for real-time sportsbook data
- **Collections:** Fixtures, Markets, Selections, Sports, Leagues
- **Write Concern:** w=0 (unacknowledged writes)
- **Retry Writes:** Enabled

### 8.2 MongoDB vs SQL Server

**MongoDB (Read-Heavy):**
- Real-time sports data (fixtures, markets, odds)
- Denormalized collection-based storage
- Optimized for rapid reads

**SQL Server (Transactional):**
- Operational backoffice data
- Configuration templates
- Activity logs
- Client settings
- Audit trails

### 8.3 Synchronization Strategy

**Bidirectional Sync:**
- SQL Server is source of truth for backoffice operations
- MongoDB is cache/replica for real-time queries
- Updates flow: API → SQL Server → Sportsbook → MongoDB
- Invalidation via RabbitMQ `OnDeleteCacheMessage`

---

## 9. SQL Server Usage

### 9.1 Database Contexts and Separation

**Multi-Database Strategy:**
Each context targets separate database for scalability:

| Context | Database | Purpose | Tables |
|---------|----------|---------|--------|
| BackofficeContext | Backoffice | Core operations | ActivityLog, ClientInfo, BetSettings, ConfigTemplate, ConfigTemplateMapping, ConfigTemplateMargin |
| TradingContext | Trading | Market/Provider mappings | SportMapping, LeagueMapping, MarketMapping, SelectionMapping, ProviderMapping |
| SportsbookContext | Sportsbook | Sports hierarchy | Fixtures, Markets, Selections, Sports, Leagues, Segments, SportLookup, MarketLookup, LeagueLookup |
| TranslationContext | Translation | Multi-language content | TranslationKeys, TranslationValues |

### 9.2 Connection String Management

**Configuration in appsettings.json:**
```json
"ConnectionStrings": {
    "Backoffice": "Data Source=stg-song88-mssql-server.cz8wmg0s28pd.ap-south-1.rds.amazonaws.com, 1433;...",
    "Trading": "Data Source=stg-song88-mssql-server.cz8wmg0s28pd.ap-south-1.rds.amazonaws.com, 1433;...",
    "Sportsbook": "Data Source=stg-song88-mssql-server.cz8wmg0s28pd.ap-south-1.rds.amazonaws.com, 1433;...",
    "Translation": "Data Source=stg-song88-mssql-server.cz8wmg0s28pd.ap-south-1.rds.amazonaws.com, 1433;..."
}
```

### 9.3 DbContext Configuration

**Connection Pooling:**
```csharp
services.AddDbContextPool<BackofficeContext>(o => 
    o.UseSqlServer(Configuration["ConnectionStrings:BackOffice"], options =>
        options.CommandTimeout(dbContextTimeout)
    )
);
```

**Timeout Configuration:**
- Custom: `Custom:DbContextTimeout` (default: 180 seconds)
- For long-running reports and bulk operations

### 9.4 Entity Framework Features Used

- **Lazy Loading:** Deferred query execution
- **Change Tracking:** Automatic entity state management
- **Transactions:** `TransactionScope` for multi-step operations
- **LINQ to SQL:** Strongly-typed queries
- **Migrations:** Code-first database schema management (not visible in codebase, likely managed separately)

---

## 10. Messaging/Event Patterns

### 10.1 Message Broker Architecture

**Provider:** RabbitMQ (v5.2.0)

**Topology:**
- **Exchange Type:** Topic (for pattern-based routing)
- **Durable:** True (messages persist across broker restarts)
- **Exclusive:** Configurable per queue
- **Priority Queues:** Supported (configurable)

### 10.2 Message Flow Architecture

```
┌─────────────────┐
│  Backoffice.API │ ──┐
│                 │   │ Publishes
└─────────────────┘   │
                      ▼
            ┌──────────────────┐
            │  RabbitMQ Broker │
            │  (Topic Exchange)│
            └──────────────────┘
                  │  ▲
     Consumes     │  │    Publishes
                  ▼  │
    ┌─────────────────────────────┐
    │  Background Services        │
    │ ┌─────────────────────────┐ │
    │ │ Config.Service (Worker) │ │
    │ │ Utility (Worker)        │ │
    │ │ WebSocket.Service       │ │
    │ └─────────────────────────┘ │
    └─────────────────────────────┘
              │
              ▼
         ┌─────────────┐
         │ WebSocket   │
         │ Hub / UI    │
         └─────────────┘
```

### 10.3 Outgoing Message Types (API → RabbitMQ)

**Activity Tracking:**
- `OnActivityLogMessage` - Single user action
- `OnActivityLogListMessage` - Batch of actions

**Configuration Updates:**
- `OnUpdateClientMarketConfigMessage` - Market-level config
- `OnUpdateClientLeagueConfigMessage` - League-level config
- `OnBulkUpdateConfigMessage` - Bulk updates
- `OnCopyClientSettingsMessage` - Copy settings to other clients

**Cache Invalidation:**
- `OnDeleteCacheMessage` - Clear specific cache keys

**Exports:**
- `OnGetManualBetSettlementHistoryMessage` - Export settlement
- `OnGetMessageSettlementHistoryMessage` - Message settlement
- `OnGetMarketSettlementHistoryMessage` - Market settlement
- `OnGetOeConfigTemplateMessage` - Config template export
- `OnConfigMyTemplateExportMessage` - Custom template export

### 10.4 Incoming Message Types (RabbitMQ → Background Services)

**Configuration Service Consumes:**
- `UpdateClientMargin` (type) - Updates margin calculations
- `DeleteConfigTemplate` (type) - Removes configuration template

**Utility Service Consumes:**
- `LogActivity` - Single activity log
- `LogActivityList` - Batch activities
- `ExportMessageSettlementHistory` - Settlement export
- `ExportManualBetSettlementHistory` - Manual settlement
- `ExportMarketSettlementHistory` - Market settlement
- `ExportOeConfigTemplate` - Config export
- `BulkUpdateConfig` - Bulk apply config
- `CopyClientSettings` - Replicate settings

**WebSocket Service:**
- Routes processed messages to connected clients via SignalR

### 10.5 Message Structure

**BasicProperties:**
```csharp
message.BasicProperties.Type = "MessageTypeName";
message.BasicProperties.CorrelationId = correlationId;
message.BasicProperties.ContentType = "application/json";
```

**Payload Format:**
```csharp
payload = JsonSerializer.Serialize(messageObject);
// Message body is UTF-8 encoded JSON
```

### 10.6 Message Processing Pattern

```csharp
private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
{
    var payload = Encoding.UTF8.GetString(e.Body);
    var messageType = e.BasicProperties.Type;
    var correlationId = e.BasicProperties.CorrelationId;
    
    switch(messageType)
    {
        case "UpdateClientMargin":
            var data = JsonSerializer.Deserialize<long>(payload);
            ProcessMessage(data);
            break;
    }
    
    // Acknowledge after successful processing
    _messageBroker.Acknowledge(e);
}
```

### 10.7 Distributed Tracing Integration

**OpenTelemetry Integration:**
```csharp
var parentContext = Propagator.Extract(default, e.BasicProperties, 
    ExtractTraceContextFromBasicProperties);

using (var activity = ActivitySource.StartActivity(
    $"{e.RoutingKey} receive", 
    ActivityKind.Consumer, 
    parentContext.ActivityContext))
{
    activity?.SetTag("messaging.system", "rabbitmq");
    activity?.SetTag("messaging.destination_kind", "queue");
    activity?.SetTag("messaging.rabbitmq.routing_key", e.RoutingKey);
    activity?.SetTag("messaging.rabbitmq.message_type", messageType);
    
    // Process message
}
```

**Tracing Context:**
- **Correlation ID:** Links related requests
- **Baggage:** Propagates user context across services
- **Activity:** Distributed trace tracking

### 10.8 RabbitMQ Configuration

**MultiRegion Support:**
```csharp
var credential = configuration.GetSection("MessageBroker:HK")
    .Get<MessageBrokerCredentialModel>();

// Connection settings per region:
// - Host, Port (5672 or 5671 for SSL)
// - Username, Password
// - VirtualHost
// - Exchange, Queue, RoutingKey
// - Consumer tag, Prefetch count
```

**Connection Properties:**
- **RequestedHeartbeat:** 60 seconds
- **SSL:** Port-based detection (5671 = SSL, 5672 = plaintext)
- **NetworkRecoveryInterval:** 5 seconds
- **AutoRecovery:** Enabled (implicit)

---

## 11. Naming Conventions

### 11.1 Project Naming

**Pattern:** `Backoffice.[Functional Area]`
- Backoffice.API
- Backoffice.Web
- Backoffice.Shared
- Backoffice.Utility
- Backoffice.Config.Service
- Backoffice.WebSocket.API
- Backoffice.WebSocket.Service

### 11.2 Namespace Naming

**Hierarchy:** `[Company].[Project].[Functional Area]`

Examples:
- `Backoffice.API.Controllers.Trading`
- `Backoffice.API.Repositories`
- `Backoffice.API.ActionFilter`
- `Backoffice.Shared.Models`
- `Backoffice.Shared.Enums`
- `Backoffice.Shared.Utilities`
- `Backoffice.Web.Services`
- `Backoffice.WebSocket.Service.Services`

### 11.3 Class Naming

**Convention:** PascalCase with domain-specific suffixes

**Repository Pattern:**
- `IXxxRepository` (interface)
- `XxxRepository` (implementation)

Examples:
- `IBackofficeRepository` / `BackofficeRepository`
- `ITradingRepository` / `TradingRepository`
- `IPlatformRepository` / `PlatformRepository`

**Service Pattern:**
- `IXxxService` (interface)
- `XxxService` (implementation)

Examples:
- `ICommonService` / `CommonService`
- `ITradingService` / `TradingService`

**Controller Pattern:**
- `XxxController` (always suffix with Controller)

Examples:
- `TradingController`
- `SettingsController`
- `AuthenticationController`

**Other Patterns:**
- `XxxContext` for DbContext (BackofficeContext)
- `IXxxFilter` for action filters (IAuthorizationFilter)
- `XxxHub` for SignalR hubs (BackofficeHub)
- `XxxMessage` for message models (OnActivityLogMessage)

### 11.4 Interface Naming

**Convention:** Prefix with `I`

- `IBackofficeRepository`
- `IMemCache`
- `IMessageBroker`
- `ISportsbookRepository`
- `IAuthorizationFilter`

### 11.5 Enum Naming

**Convention:** PascalCase, no "Enum" suffix

- `StatusCode`
- `BetStatus`
- `Activity`
- `AlertStatus`
- `Currency`
- `Access`

### 11.6 Database Entity Naming

**Table Names:** PascalCase (no prefix)
- ActivityLog
- ClientInformation
- ConfigTemplate
- ClientBetSetting

**Foreign Key Convention:**
- `[NavigationProperty]Id` (implicit by Entity Framework)
- Example: `ClientId`, `TemplateId`

### 11.7 Method Naming

**Convention:** PascalCase, descriptive verb-first pattern

**Repository Methods:**
- `GetXxx(criteria)` - Query operations
- `AddXxx(model)` - Create operations
- `UpdateXxx(model)` - Modify operations
- `DeleteXxx(id)` - Remove operations
- `ProcessXxx(message)` - Background processing

**Examples:**
- `GetActivityLog(id)`
- `UpdateClientMargin(clientId, margin)`
- `ProcessActivityLog(activityLog)`
- `DeleteConfigTemplate(templateId)`

### 11.8 HTTP Route Naming

**Convention:** Lowercase, slash-separated, resource-oriented

**Backoffice.API Routes:**
```
POST   /api/authentication/login
POST   /api/backoffice/activity-log
GET    /api/trading/market-mapping/{id}
POST   /sportsbook/client/list
GET    /sportsbook/settings/{clientId}
```

**Route Parameters:**
- PascalCase in method parameters
- kebab-case in route templates (convention not strictly followed)

### 11.9 Configuration Key Naming

**Convention:** Hierarchical with colons, PascalCase segments

```
ConnectionStrings:BackOffice
ConnectionStrings:Trading
API:AuthenticationAPI:Uri
API:PlatformAPI:HK:ApiKey
MessageBroker:HK:Host
MessageBroker:HK:Username
Custom:DbContextTimeout
Telemetry:Environment
```

### 11.10 Cache Key Naming

**Pattern:** Domain-aware, separator-based construction

Examples:
- `client_{clientId}_margins`
- `market_config_{marketId}`
- `league_settings_{leagueId}`
- `template_{templateId}_export`

---

## 12. Coding Standards

### 12.1 Code Organization

**File Structure per Project:**
```
Project/
├── Controllers/
│   ├── [DomainArea]/
│   │   └── [Entity]Controller.cs
├── Repositories/
│   ├── I[Entity]Repository.cs
│   └── [Entity]Repository.cs
├── Services/
│   ├── I[Service]Service.cs
│   └── [Service]Service.cs
├── Models/
│   ├── Requests/
│   ├── Response/
│   └── [Entity]Model.cs
├── ActionFilter/
│   └── [FilterName]Attribute.cs
├── Hubs/
│   └── [Entity]Hub.cs
├── Telemetry/
│   └── Instrumentation.cs
├── Program.cs
├── Startup.cs
└── appsettings.json
```

### 12.2 Dependency Injection Standard

**Always Use Constructor Injection:**
```csharp
// ✓ Correct
public class UserController
{
    private readonly IUserRepository _repository;
    
    public UserController(IUserRepository repository)
    {
        _repository = repository;
    }
}

// ✗ Avoid: Service Locator
public class UserController
{
    private readonly IServiceProvider _serviceProvider;
    public void SomeMethod()
    {
        var repo = _serviceProvider.GetService<IUserRepository>();
    }
}
```

### 12.3 Response Pattern

**Standardized Response Format:**
```csharp
// Success
return new BaseResponse<ClientList>
{
    StatusCode = StatusCode.Success,
    Message = "Operation completed successfully",
    Result = clientList
};

// Error
return new BaseResponse<ClientList>
{
    StatusCode = StatusCode.Error,
    Message = "An error occurred",
    Result = null
};

// Validation Error
return new BaseResponse<ClientList>
{
    StatusCode = StatusCode.Validation_Error,
    Message = "Invalid input data",
    Result = null
};
```

### 12.4 Repository Methods

**Pattern: Async/Await**
```csharp
public async Task<BaseResponse<T>> GetXxxAsync(int id)
{
    try
    {
        var result = await _context.Xxx.FirstOrDefaultAsync(x => x.Id == id);
        if (result == null)
            return new BaseResponse<T> { StatusCode = StatusCode.Not_Found };
        
        return new BaseResponse<T>
        {
            StatusCode = StatusCode.Success,
            Result = result
        };
    }
    catch (Exception ex)
    {
        // Log exception
        return new BaseResponse<T> { StatusCode = StatusCode.Error };
    }
}
```

### 12.5 Exception Handling

**Pattern: Try-Catch with Logging**
```csharp
try
{
    // Business logic
}
catch (DbUpdateException ex)
{
    _logger.LogError("Database error: {message}", ex.Message);
    return new BaseResponse<T> { StatusCode = StatusCode.Error };
}
catch (Exception ex)
{
    _logger.LogError("Unexpected error: {exception}", ex);
    return new BaseResponse<T> { StatusCode = StatusCode.Error };
}
```

**No Silent Failures:** Always log and return structured response

### 12.6 Logging Standards

**Provider:** Serilog with structured logging

**Log Levels:**
- `Information` - Application flow milestones
- `Warning` - Recoverable issues
- `Error` - Exceptions and failures
- `Debug` - Development troubleshooting (not in production)

**Structured Logging Example:**
```csharp
_logger.LogInformation("Client margin updated: clientId={clientId}, margin={margin}", 
    clientId, margin);

_logger.LogError("Failed to process message: type={messageType}, error={error}",
    messageType, exception.Message);
```

### 12.7 Async/Await Convention

**All I/O Operations Must Be Async:**
- Database queries: `await _context.Xxx.ToListAsync()`
- HTTP calls: `await _httpClient.GetAsync()`
- Message publishing: `await _messageBroker.PublishAsync()`

**Task Return Types:**
- Operations with return value: `Task<T>`
- Operations without return: `Task` (not `async void`)

```csharp
// ✓ Correct
public async Task<BaseResponse<T>> GetXxxAsync()
{
    return await _repository.GetAsync();
}

// ✗ Avoid
public async void GetXxx()
{
    await _repository.GetAsync();
}
```

### 12.8 Null Checking

**Pattern: Early Return**
```csharp
public async Task<BaseResponse<T>> ProcessXxx(XxxRequest request)
{
    if (request == null)
        return new BaseResponse<T> { StatusCode = StatusCode.Validation_Error };
    
    if (string.IsNullOrEmpty(request.Name))
        return new BaseResponse<T> { StatusCode = StatusCode.Validation_Error };
    
    // Continue processing
}
```

### 12.9 LINQ Standard

**Usage:** LINQ to Entities (via EF Core), not LINQ to Objects when querying DB

```csharp
// ✓ Correct - Executed in database
var clients = await _context.Clients
    .Where(c => c.IsActive)
    .OrderBy(c => c.Name)
    .ToListAsync();

// ✗ Avoid - Loads all data then filters in-memory
var allClients = await _context.Clients.ToListAsync();
var filtered = allClients.Where(c => c.IsActive).ToList();
```

### 12.10 String Interpolation

**Convention:** Use string interpolation, not string concatenation

```csharp
// ✓ Correct
var message = $"Client {clientId} was updated at {DateTime.Now}";

// ✗ Avoid
var message = "Client " + clientId + " was updated at " + DateTime.Now;
```

### 12.11 JSON Serialization

**Provider:** System.Text.Json

**Configuration:**
```csharp
// Case-insensitive deserialization
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

// Enum handling
services.AddControllers().AddJsonOptions(opt =>
{
    opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ReferenceLoop handling (Newtonsoft JSON)
options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
```

### 12.12 Security Standards

**API Keys:**
- Always read from configuration, never hardcode
- Use header-based transmission: `x-api-key`
- Rotate regularly

**JWT Tokens:**
- Extracted from `Authorization: Bearer {token}` header
- Validated in AuthorizeAttribute
- Claims propagated for authorization decisions

**Database Credentials:**
- Environment-specific in appsettings.[Environment].json
- Connection pooling with appropriate timeouts

### 12.13 Validation Standards

**Input Validation:**
```csharp
// Null check
if (model == null) return BadRequest("Model is required");

// String validation
if (string.IsNullOrWhiteSpace(model.Name)) 
    return BadRequest("Name is required");

// Range validation
if (model.Value < 0 || model.Value > 100) 
    return BadRequest("Value must be between 0 and 100");

// Reference validation
var entity = await _repository.GetAsync(model.Id);
if (entity == null) return NotFound("Entity not found");
```

### 12.14 Commenting Standards

**Comments Are Minimal (Code Should Be Self-Documenting):**

```csharp
// ✓ Correct - Clear intent
public async Task<BaseResponse<bool>> UpdateClientMarginAsync(int clientId, decimal margin)
{
    // Recalculate based on global margin adjustment
    var adjustedMargin = margin * _globalMultiplier;
    return await _repository.UpdateAsync(clientId, adjustedMargin);
}

// ✗ Avoid - Redundant
// Update the client margin
// params: clientId = client identifier, margin = margin value
// returns: success response
public async Task<BaseResponse<bool>> UpdateClientMarginAsync(int clientId, decimal margin)
{
    var adjustedMargin = margin * _globalMultiplier;
    return await _repository.UpdateAsync(clientId, adjustedMargin);
}
```

**Exception Comments:**
```csharp
// Note: Will Remove
public async Task<BaseResponse<bool>> AddActivityLog(ActivityLogModel request)
{
    // This method is deprecated, use new logging system instead
}
```

### 12.15 Access Modifiers

**Pattern: Explicit Access Control**
```csharp
// Repository field - private, readonly
private readonly BackofficeContext _context;

// Public interface - only public methods on external contracts
public interface IXxxRepository
{
    Task<BaseResponse<T>> GetXxxAsync(int id);
}

// Controller - public
public class XxxController : ControllerBase
{
    // Private helper
    private BaseResponse<T> ValidateInput(XxxRequest request) { }
}
```

---

## 13. Architecture Reference Implementations

### 13.1 Similar Solutions in Reference Folder

**Auth Service** (Reference/Auth/)
- Similar DI and Startup pattern
- Identity/Roles based authorization
- JWT token generation
- Same Serilog telemetry setup

**Platform Service** (Reference/platform-final/)
- Repository pattern
- Multi-HttpClient configuration
- Response compression (Brotli/Gzip)
- Forward headers for proxy support

**Odds Engine** (Reference/odds-engine-2/)
- Likely contains message-driven architecture
- Event processing for odds updates

### 13.2 Common Patterns Across Ecosystem

**Standard Startup Chain:**
1. Serilog configuration
2. Telemetry/Instrumentation
3. Database contexts
4. Repositories and Services
5. HttpClients
6. CORS
7. Swagger

**Standard Security:**
- API Key header validation (`x-api-key`)
- JWT Bearer token support
- Action filter based authorization

**Standard Response Format:**
- `BaseResponse<T>` wrapper
- Consistent status code enumeration
- Message and Result fields

---

## 14. Data Flow Diagrams

### 14.1 Request Flow: Web UI to Data

```
User (Blazor Browser)
        │
        │ WebSocket/SignalR
        ▼
Backoffice.Web (Blazor Server)
        │
        │ HTTP REST
        ▼
Backoffice.API
        │
        ├─► IBackofficeRepository ──► BackofficeContext (SQL)
        │
        ├─► IPlatformRepository ──► Platform.API ──► Platform DB
        │
        ├─► ITradingRepository ──► TradingContext (SQL)
        │
        └─► IRabbitMqRepository ──► RabbitMQ ──► Message Broker
```

### 14.2 Asynchronous Message Flow

```
Backoffice.API
    │
    │ Publishes (OnActivityLogMessage)
    ▼
RabbitMQ Topic Exchange
    │
    ├─► Backoffice.Utility ─┐
    │                       │ Process Message
    │                       ▼
    │                  IBackofficeRepository
    │                       │
    ├─► Backoffice.Config.Service ──► Database Updates
    │
    └─► Backoffice.WebSocket.Service
            │
            ▼
        BackofficeHub ──► SignalR ──► WebSocket Clients
            │
            ▼
        Backoffice.Web ──► Blazor UI ──► Browser
```

### 14.3 Authentication Flow

```
Client
  │
  │ POST /api/authentication/login
  ▼
Backoffice.API (AuthenticationController)
  │
  │ Delegates to AuthenticationRepository
  ▼
Authentication.API (External)
  │
  │ Validates credentials
  ▼
JWT Token returned
  │
  ▼
Client stores token
  │
  │ Subsequent requests with: Authorization: Bearer {token}
  ▼
Backoffice.API (AuthorizeAttribute)
  │
  │ Extracts claims from token
  ▼
Validates authorization
  │
  ├─► Allowed ──► Process request
  │
  └─► Denied ──► Return 401 Unauthorized
```

### 14.4 Real-Time Update Flow

```
Configuration Change
  │
  ├─► Backoffice.API (Update handler)
  │   │
  │   ├─► Database update
  │   │
  │   └─► Publish OnUpdateClientMarketConfigMessage
  │       │
  ▼       ▼
RabbitMQ
  │
  ├─► Backoffice.WebSocket.Service (Consumer)
  │   │
  │   └─► Process message
  │       │
  ▼       ▼
BackofficeHub (SignalR)
  │
  ├─► Broadcast to all connected clients
  │
  ├─► Backoffice.Web (Blazor)
  │   │
  │   └─► Update UI component
  │       │
  └─────► Browser (Real-time display)

AND

  ├─► Backoffice.Utility (Consumer)
  │   │
  │   └─► Process and cache invalidation
  │       │
  ▼       ▼
Publish OnDeleteCacheMessage
  │
  └─► Cache cleared across services
```

---

## 15. Deployment Architecture

### 15.1 Deployment Units

**Containerized Services:**
- Each project has a Dockerfile
- Docker Compose orchestration
- Kubernetes-ready structure

**Service Mapping:**
- **Backoffice.API** → Container port 80/443
- **Backoffice.Web** → Container port 80/443
- **Backoffice.WebSocket.API** → Container port 80/443
- **Backoffice.Config.Service** → Container port (no HTTP)
- **Backoffice.Utility** → Container port (no HTTP)
- **Backoffice.WebSocket.Service** → Container port (no HTTP)

### 15.2 Infrastructure Dependencies

**External Services:**
- SQL Server (4 databases)
- MongoDB (Sportsbook replica)
- RabbitMQ (Message broker)
- Authentication API
- Platform API
- Sportsbook API
- Odds Engine API

**Configuration per Environment:**
- Development: Local/Docker instances
- Staging: AWS RDS + external services
- Production: High-availability setup

---

## 16. Security Architecture

### 16.1 Authentication

**Methods Supported:**
1. **API Key** - Identified by header `x-api-key`
2. **JWT Bearer Token** - From Auth API
3. **Custom Authorization** - Claim-based via AuthorizeAttribute

### 16.2 Authorization

**Three-Layer Authorization:**

1. **ApiKeyFilter** - Endpoint-level security check
   ```csharp
   [ServiceFilter(typeof(ApiKeyFilter))]
   public class SomeController : ControllerBase { }
   ```

2. **AuthorizeAttribute** - Method-level claim validation
   ```csharp
   [Authorize] // or [Authorize("claimType", value)]
   public async Task<IActionResult> SomeAction() { }
   ```

3. **Repository-level** - Business rule validation
   ```csharp
   if (!user.HasAccess(resource))
       return Forbidden();
   ```

### 16.3 Multi-Region Security

**Region-Aware Configuration:**
```json
"API": {
    "PlatformAPI": {
        "HK": { "APIKey": "xxx", "Uri": "https://..." },
        "SA": { "APIKey": "yyy", "Uri": "https://..." }
    }
}
```

**Region Routing:**
```csharp
var region = context.HttpContext.Request.Headers["x-region"];
var apiKey = Configuration[$"API:XxxAPI:{region}:APIKey"];
```

---

## 17. Performance Considerations

### 17.1 Caching Strategy

- **In-Process Memory Cache** with TTL
- **Cache Invalidation** via RabbitMQ messaging
- **Lazy-loading** pattern for expensive queries

### 17.2 Database Optimization

- **Connection Pooling** - DbContextPool
- **Async I/O** - All database access is async
- **Command Timeout** - 180 seconds for long-running queries
- **Batch Operations** - Bulk insert/update via APIs

### 17.3 Message Processing

- **Prefetch Count** - RabbitMQ consumer prefetch limit
- **Asynchronous Processing** - Non-blocking message consumption
- **Distributed Tracing** - Activity tracking for bottleneck identification

### 17.4 WebSocket Optimization

- **Message Compression** - Brotli/Gzip
- **MessagePack Serialization** - Binary format for efficiency
- **Buffer Management** - 5MB maximum message size
- **Circuit Retention** - 3-minute retention for disconnected clients

---

## 18. Key Findings and Recommendations

### 18.1 Strengths

✓ **Well-Structured:** Clear separation of concerns through repositories and services  
✓ **Enterprise Patterns:** DI, async/await, structured logging  
✓ **Scalable:** Microservices-ready with async messaging  
✓ **Secure:** Multi-layer authentication and authorization  
✓ **Observable:** OpenTelemetry integration for distributed tracing  
✓ **Maintainable:** Consistent naming conventions and coding standards  

### 18.2 Optimization Opportunities

⚠ **Consideration Areas:**

1. **Entity Framework Query Optimization**
   - Some repositories have complex multi-step queries
   - Consider query analysis and optimization

2. **Cache Invalidation Strategy**
   - Currently RabbitMQ-dependent
   - Consider distributed cache (Redis) for production

3. **Error Handling Consistency**
   - Some methods use try-catch, others don't
   - Centralized exception middleware could improve consistency

4. **API Versioning**
   - Not currently implemented
   - Consider for backward compatibility as API evolves

5. **Rate Limiting**
   - No rate limiting on public endpoints
   - Consider implementation for DDoS protection

### 18.3 Documentation Gaps

- No API documentation beyond Swagger
- No database schema documentation
- Limited inline code documentation

---

## 19. Technology Stack Summary

| Component | Technology | Version |
|-----------|-----------|---------|
| Framework | .NET Core | 3.1 |
| Web Framework | ASP.NET Core | 3.1 |
| Web Frontend | Blazor Server | 3.1 |
| ORM | Entity Framework Core | 3.1.15 |
| Primary Database | SQL Server | 2019+ |
| Secondary Database | MongoDB | 4.0+ |
| Message Broker | RabbitMQ | 3.8+ |
| Real-time Communication | SignalR / WebSocket | 3.1 |
| Logging | Serilog | 4.3.0 |
| Observability | OpenTelemetry | 1.5.0 |
| Serialization | System.Text.Json | 6.0.7 |
| Serialization (Binary) | MessagePack | - |
| Compression | Brotli/Gzip | - |
| Local Caching | System.Runtime.Caching | 7.0.0 |
| JWT | System.IdentityModel.Tokens.Jwt | - |
| API Documentation | Swagger/OpenAPI | 6.4.0 |
| Client Libraries | UAParser | 3.1.47 |

---

## 20. Conclusion

The Backoffice solution demonstrates a modern, enterprise-grade architecture for sports betting operations management. It successfully combines RESTful APIs, real-time WebSocket communication, asynchronous message processing, and comprehensive observability into a cohesive system.

The architecture is well-positioned for:
- **Horizontal Scaling** through stateless microservices
- **Feature Development** with clear patterns and conventions
- **Operational Monitoring** via OpenTelemetry integration
- **Multi-Region Deployment** with region-aware configuration
- **Real-Time Operations** through WebSocket and SignalR

Future enhancements should focus on distributed caching, advanced API versioning, and enhanced error recovery mechanisms for production-grade reliability.

---

**Report Generated:** June 18, 2026  
**Analysis Completed By:** GitHub Copilot  
**Framework Version:** .NET Core 3.1  
**Last Updated:** June 18, 2026
