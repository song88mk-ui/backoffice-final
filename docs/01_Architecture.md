# Architecture Overview

**Document Status:** Analysis Complete (No Code Changes Made)  
**Date:** June 18, 2026  
**Framework:** .NET Core 3.1

## Architecture Pattern: Multi-Tier Event-Driven Microservices

The Backoffice solution implements a **distributed, event-driven architecture** with the following characteristics:

### Architectural Layers

```
┌─────────────────────────────────────────────────────────────┐
│                   Presentation Layer                         │
│              (Blazor Server Web Application)                 │
│                Backoffice.Web                                │
└─────────────────────────────────────────────────────────────┘
                            ▲
                            │ HTTP/REST
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway Layer                         │
│            (Primary REST API Endpoints)                      │
│                Backoffice.API                                │
│       (Controllers, Repositories, Services)                  │
└─────────────────────────────────────────────────────────────┘
            ▲                   ▲                   ▲
            │                   │                   │
       HTTP │              RabbitMQ │            SQL │
            │                   │                   │
            ▼                   ▼                   ▼
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│  External APIs   │ │ Message Broker   │ │  Data Access     │
│  (Platform,      │ │ (RabbitMQ)       │ │  (SQL Server)    │
│   Sportsbook,    │ │                  │ │  (MongoDB)       │
│   Auth, Trading) │ │  Background      │ │                  │
│                  │ │  Workers:        │ │  Contexts:       │
│                  │ │  - Config.Svc    │ │  - Backoffice    │
│                  │ │  - Utility       │ │  - Trading       │
│                  │ │  - WebSocket.Svc │ │  - Sportsbook    │
│                  │ │                  │ │  - Translation   │
└──────────────────┘ └──────────────────┘ └──────────────────┘
```

### Execution Model

```
Synchronous                          Asynchronous
─────────────────────────────────────────────────────────
User Request → Blazor UI            ┐
    ↓                                ├─→ RabbitMQ ──→ Config.Service
User Action → Backoffice.API        │           └─→ Utility
    ↓                                │           └─→ WebSocket.Service
Repository Query                    ↓
    ↓                           Background Processing
Database/Cache/External APIs         ↓
    ↓                           WebSocket Hub
Response to Client                   ↓
                                  Connected Clients
```

## Component Organization

### Tier 1: Presentation
- **Backoffice.Web** - Server-side Blazor application
- Real-time UI updates via SignalR
- Local caching with Blazored.LocalStorage

### Tier 2: API Gateway
- **Backoffice.API** - REST API server
- Request validation and routing
- Orchestration of repositories and services
- Authentication and authorization

### Tier 3: Real-Time Communication
- **Backoffice.WebSocket.API** - SignalR hub for WebSocket connections
- Message compression (MessagePack)
- Multi-transport fallback (WebSocket → SSE → LongPolling)

### Tier 4: Background Processing
- **Backoffice.Config.Service** - Configuration management worker
- **Backoffice.Utility** - General-purpose background tasks
- **Backoffice.WebSocket.Service** - Message distribution service
- All consume from RabbitMQ topic exchanges

### Tier 5: Data Access
- **Backoffice.Shared** - Shared library
  - Entity Framework DbContexts (4 databases)
  - Domain models and DTOs
  - Utilities and helpers
  - Message definitions

### Tier 6: External Systems
- Platform API (client configuration)
- Sportsbook API (fixtures and markets)
- Authentication API (user validation)
- Trading API (odds engine integration)
- RabbitMQ (message broker)
- SQL Server (4 databases)
- MongoDB (sportsbook cache)

## Dependency Flow

### Top-Down (Request Flow)
```
Blazor UI Component
    ↓
Blazor Service Layer (IXxxService)
    ↓
HTTP Call to Backoffice.API/Controller
    ↓
API Controller Action
    ↓
Repository Abstraction (IXxxRepository)
    ↓
Repository Implementation
    ↓ (branches to)
    ├─ DbContext (SQL Server queries)
    ├─ HttpClient (External APIs)
    ├─ IMemCache (Caching layer)
    └─ IRabbitMqRepository (Message publishing)
```

### Event Flow (Asynchronous)
```
API Operation → RabbitMQ Publish
    ↓
Config.Service / Utility / WebSocket.Service Consume
    ↓
Process & Respond via
    ├─ Database Updates
    ├─ RabbitMQ Re-Publish
    └─ WebSocket Broadcast
```

## Communication Patterns

### Synchronous (HTTP/REST)
- Client → Backoffice.API (REST calls)
- Backoffice.API → External APIs (HttpClient)
- Backoffice.Web ← Backoffice.API (HTTP responses)
- Browser ← Backoffice.Web (HTML/CSS/JS)

### Asynchronous (RabbitMQ)
- Backoffice.API → RabbitMQ (MessageRequest)
- Background Services → RabbitMQ (consume)
- Background Services → RabbitMQ (publish results)
- Backoffice.WebSocket.Service → SignalR Hub → Clients

### Real-Time (WebSocket/SignalR)
- Backoffice.Web → BackofficeHub (subscription)
- BackofficeHub → Connected Clients (broadcast)
- Multiple transports supported

## Multi-Region Architecture

The system supports multiple regions with region-specific configuration:

```
┌─────────────────────────────────────────┐
│       Request Handler (API)             │
│                                         │
│  Extract Region from:                   │
│  - Header: x-region                     │
│  - Configuration                        │
│  - Request context                      │
└─────────────────────────────────────────┘
                    ↓
    ┌───────────────┬───────────────┐
    ▼               ▼               ▼
   HK             SA             Default
┌──────────┐  ┌──────────┐  ┌──────────┐
│ Config   │  │ Config   │  │ Config   │
│ URLs     │  │ URLs     │  │ URLs     │
│ Keys     │  │ Keys     │  │ Keys     │
│ Params   │  │ Params   │  │ Params   │
└──────────┘  └──────────┘  └──────────┘
```

Region-aware services:
- RabbitMQ connections (HK, SA)
- Platform API endpoints
- Trading API endpoints
- Sportsbook API endpoints

## Scaling Characteristics

### Horizontal Scaling
- **Stateless API Servers** - Can run multiple instances
- **Background Workers** - Each consumes independently from RabbitMQ
- **WebSocket Hub** - Scales with Signalr connection management

### Vertical Scaling
- **Memory Caching** - In-process, limited by single server RAM
- **Database Connections** - Pooled with DbContextPool
- **Long-running Queries** - 180-second timeout

### Bottleneck Areas
1. **MongoDB Connection** - Shared static reference
2. **RabbitMQ Consumer Prefetch** - Configurable per worker
3. **SQL Connection Pooling** - Limited by database
4. **WebSocket Connections** - SignalR scalability limits

## Configuration Management

### Configuration Sources (Startup Order)
1. `appsettings.json` - Base defaults
2. `appsettings.{Environment}.json` - Environment-specific
3. Environment variables - Runtime overrides
4. Command-line arguments - Explicit overrides

### Configuration Categories
- **ConnectionStrings** - Database connection strings (4 databases)
- **API** - External service endpoints and API keys
- **Security** - API key and JWT settings
- **Custom** - Application-specific settings
- **Logging** - Serilog configuration
- **Telemetry** - OpenTelemetry settings

## Cross-Cutting Concerns

### Authentication & Authorization
- API Key validation via `ApiKeyFilter`
- JWT Bearer token support via `AuthorizeAttribute`
- Claim-based authorization in controllers

### Logging
- Centralized via Serilog
- Structured logging with context
- Log levels: Information, Warning, Error, Debug

### Monitoring & Observability
- OpenTelemetry ActivitySource for distributed tracing
- Performance counters for key operations
- Health check endpoints

### Exception Handling
- Try-catch in repositories with error wrapping
- BaseResponse<T> envelope for all responses
- StatusCode enumeration for result classification

### Caching Strategy
- In-process MemoryCache (IMemCache)
- TTL-based expiration
- Manual invalidation via RabbitMQ messages

## Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Framework | .NET Core | 3.1 |
| Web API | ASP.NET Core | 3.1 |
| Frontend | Blazor Server | 3.1 |
| ORM | EF Core | 3.1.15 |
| Real-time | SignalR | 3.1 |
| Messaging | RabbitMQ Client | 5.2.0 |
| Database | SQL Server | 2019+ |
| Cache | MongoDB | 4.0+ |
| Logging | Serilog | 4.3.0 |
| Tracing | OpenTelemetry | 1.5.0 |

## Deployment Model

### Container-Based
- Each service has Dockerfile
- Designed for Kubernetes/Docker orchestration
- Stateless application design

### Network Assumptions
- All services accessible via network
- SQL Server available at configured endpoints
- MongoDB accessible from application
- RabbitMQ broker accessible
- External APIs reachable

### Security Assumptions
- TLS/SSL for external communications
- API keys managed externally
- Database credentials in secrets management
- JWT tokens valid for authentication

## System Constraints

### Technology Constraints
- .NET Core 3.1 (older framework, may need upgrade)
- Single-region deployment per instance
- In-process caching limits scalability
- Static MongoDB client not thread-safe by design

### Operational Constraints
- 180-second database operation timeout
- RabbitMQ dependency for async operations
- Synchronous database transactions for consistency
- Static collections for MongoDB queries

### Business Constraints
- Multi-tenant by region (HK, SA)
- Configuration-driven market settings
- Real-time odds and fixture updates
- Activity audit trail requirements

## Future Architecture Considerations

### Potential Improvements
1. Distributed caching (Redis) instead of in-process
2. API Gateway pattern (Kong, Azure API Manager)
3. Microservices decomposition
4. Event sourcing for audit trails
5. CQRS pattern for read/write separation
6. Containerized deployment orchestration
7. Service mesh (Istio) for inter-service communication
8. API versioning strategy
