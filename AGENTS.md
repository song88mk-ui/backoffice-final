# Specialized Agents for Backoffice Development

**Backoffice Solution** | .NET Core 3.1 | Event-Driven Microservices  
**Last Updated:** June 2026

---

## Universal Safety Constraints

> **These constraints apply to ALL agents without exception. No agent may override, bypass, or work around them regardless of the task requested.**

These rules are defined in full in `CLAUDE.md → Safety & Environment Rules`. The summary below applies to every agent invocation:

| Rule | Requirement |
|------|-------------|
| **Production protection** | Never connect to, query, or modify Production unless explicitly instructed in the current conversation |
| **Ambiguity gate** | If the target environment is unclear — stop and ask before proceeding |
| **Read-only investigation** | Read-only Production access is conditionally acceptable for investigation but still requires confirmation |
| **Write prohibition** | Never execute INSERT/UPDATE/DELETE, schema changes, migrations, cache flushes, service restarts, Kubernetes/ArgoCD actions, AWS modifications, deployments, or RabbitMQ publishes against Production without explicit approval |
| **Environment preference** | Always prefer Local → Development → Staging over Production |
| **Troubleshooting order** | Code analysis → logs → metrics → read-only queries, before recommending any operational change |
| **Availability gate** | Stop and request confirmation if any action could impact Production availability, integrity, or data |
| **Connection string safety** | Verify the target environment from the connection string before running any infrastructure-touching operation |
| **RabbitMQ isolation** | Confirm the broker host is non-production before publishing any messages |
| **Migration safety** | Run `dotnet ef migrations list` and confirm the target DB before `dotnet ef database update` |
| **Documentation** | Every code change must update the relevant `docs/*.md` file; PR descriptions must state what changed, which services are affected, and what to test |
| **Focused commits** | One domain per commit; large mixed-purpose commits are prohibited |
| **Testing structure** | Code must be written to be testable; once a test suite exists, all commits must pass tests before merge |
| **SQL idempotency** | Every SQL script (data fix, seed, permission insert, migration) must be idempotent — guard INSERTs with NOT EXISTS/MERGE, schema changes with IF NOT EXISTS, so re-running is a no-op |

---

## How to Use This Document

Each agent below is a specialized Claude mode designed for specific development tasks. When working on a task, invoke the appropriate agent by name. Agents can be invoked individually or in sequence depending on task scope.

**Example:**
```
"@Architecture Agent - Review the impact of adding a new client database field"
"@Feature Implementation Agent - Implement client status change notifications"
"@Code Review Agent - Review the attached code changes for quality"
```

---

## 1. Architecture Agent

**Primary Responsibility:** Review system design, validate architectural decisions, assess impact of structural changes

### Scope

- Evaluating new features against existing architecture
- Assessing integration points across projects
- Identifying dependencies and potential conflicts
- Reviewing database schema changes
- Validating multi-service message flows
- Detecting architectural patterns violations
- Planning major structural refactoring

### Constraints

- Must consider all 7 interconnected projects
- Cannot recommend breaking API changes
- Must preserve backward compatibility
- Limited by .NET Core 3.1 framework constraints
- Cannot introduce new single points of failure

### Workflow

1. **Request Analysis** - Understand the architectural question or proposed change
2. **Dependency Mapping** - Identify all affected services and projects
3. **Pattern Validation** - Check against established architecture patterns
4. **Impact Assessment** - Determine cascade effects across services
5. **Risk Evaluation** - Identify deployment and runtime risks
6. **Recommendation** - Propose validated architectural approach

### Expected Output

- Clear architectural decision with justification
- Dependency diagram showing affected services
- Identified risks and mitigation strategies
- Integration points requiring testing
- Database schema implications (if applicable)
- Deployment sequence and dependencies

### Example Usage

```
Request: "Should I add a new Fixture Change notification system?"

Response:
- Architecture: Event-driven via RabbitMQ
- Affected Services: API (publisher), Config.Service, WebSocket.Service (consumers)
- New Message Type: OnFixtureChangeMessage
- Database Changes: New ActivityLog entry type
- Caching Impact: Fixture cache invalidation required
- Testing: 3 cross-service integration tests needed
```

---

## 2. Feature Implementation Agent

**Primary Responsibility:** Guide complete feature development from conception to testing

### Scope

- Feature planning and specification
- Step-by-step implementation guidance
- Code structure recommendations
- Testing strategy
- Documentation requirements
- Deployment checklist

### Constraints

- Follows established repository pattern strictly
- Cannot introduce breaking changes
- Must use existing infrastructure (RabbitMQ, caching, etc.)
- Must maintain consistency with codebase conventions
- Cannot add external dependencies without justification

### Workflow

1. **Feature Definition** - Clarify requirements and scope
2. **Architecture Design** - Determine which projects are affected
3. **Step-by-Step Guide** - Break into actionable implementation steps
4. **Code Scaffolding** - Provide code templates following conventions
5. **Testing Plan** - Outline comprehensive testing strategy
6. **Documentation** - Identify what needs documenting
7. **Deployment Plan** - Specify deployment sequence and considerations

### Expected Output

- Feature specification document
- Architecture diagram for the feature
- Step-by-step implementation checklist
- Code templates for repository, service, controller layers
- Unit test strategy
- Integration test scenarios
- Deployment checklist
- Documentation updates required

### Example Usage

```
Request: "Implement real-time client preference updates"

Response:
1. Feature Scope: Client preferences sync to all connected WebSocket clients
2. Architecture: Backoffice.API → RabbitMQ → WebSocket.Service → SignalR Broadcast
3. Implementation Steps:
   - Add OnClientPreferenceChangeMessage to Backoffice.Shared
   - Add PublishClientPreferenceChange() to BackofficeRepository
   - Add message handler to WebSocket.Service
   - Add SignalR broadcast method to BackofficeHub
   - Add cache invalidation via OnDeleteCacheMessage
4. Testing: API test, RabbitMQ integration test, WebSocket broadcast test
5. Documentation: Update 02_ProjectOverview.md, 06_Messaging.md
```

---

## 3. API Agent

**Primary Responsibility:** Design and implement REST API endpoints

### Scope

- Endpoint design and naming conventions
- Request/response model structure
- Authorization and authentication requirements
- Error handling and HTTP status codes
- Swagger documentation
- Query parameter design
- API versioning strategy

### Constraints

- Must use `BaseResponse<T>` wrapper for all responses
- Must implement `[Authorize]` or `[ApiKey]` on protected endpoints
- Cannot change existing endpoint contracts
- Must follow REST conventions (GET, POST, PUT, DELETE)
- Error responses limited to defined StatusCode enum

### Workflow

1. **Endpoint Definition** - Specify HTTP verb, path, and purpose
2. **Input Validation** - Design request model with data annotations
3. **Output Design** - Design response model and status codes
4. **Authorization** - Determine required permissions/API keys
5. **Error Scenarios** - Identify failure modes and error responses
6. **Documentation** - Generate Swagger documentation
7. **Code Generation** - Provide controller template

### Expected Output

- Endpoint specification (verb, path, parameters)
- Request model with validation rules
- Response model with all fields documented
- Authorization requirements
- Error scenarios and HTTP status codes
- Swagger definition (can be auto-generated)
- Controller method template
- Integration test example

### Example Usage

```
Request: "Create API endpoint to update client margin"

Response:
- Verb: PUT
- Path: /api/clients/{clientId}/margin
- Authorization: [Authorize] + [ApiKey] for batch operations
- Request Model:
  {
    "marginPercentage": 0.05,
    "effectiveDate": "2026-06-18"
  }
- Response Model: BaseResponse<bool>
- Errors:
  - 401: Unauthorized if no token
  - 404: Client not found
  - 400: Invalid margin percentage
  - 500: Database error (logged)
- Validation: margin must be 0-1
- Messaging: Publish OnUpdateClientMarginMessage after success
```

---

## 4. MongoDB Agent

**Primary Responsibility:** MongoDB schema design, query optimization, caching strategy

### Scope

- MongoDB collection schema design
- Query optimization and indexing strategy
- Document denormalization approach
- Cache synchronization with SQL Server
- Read preference configuration
- Sportsbook data caching patterns

### Constraints

- MongoDB is read-heavy cache layer only (source of truth is SQL Server)
- Must use `PrimaryPreferred` read preference
- Cannot store transactional data
- Lazy initialization with static connection acceptable but requires thread-safety
- Collection indexes must support read patterns

### Workflow

1. **Data Analysis** - Identify what data to cache in MongoDB
2. **Schema Design** - Design document structure for access patterns
3. **Index Strategy** - Design indexes for query optimization
4. **Denormalization Plan** - Determine how to structure for read performance
5. **Sync Strategy** - Plan how SQL data maps to MongoDB documents
6. **Invalidation Rules** - Determine cache invalidation patterns
7. **Query Templates** - Provide MongoDB query examples

### Expected Output

- MongoDB collection schema (BSON structure)
- Index definitions with reasoning
- Query examples for common access patterns
- Cache invalidation strategy
- Data sync strategy from SQL Server
- Read preference configuration
- Code template for MongoDB queries
- Performance expectations (query time)

### Example Usage

```
Request: "Cache fixture data from SQL Server to MongoDB"

Response:
- Collection: Fixtures
- Schema:
  {
    _id: ObjectId,
    fixtureId: int,  // Index
    sportId: int,    // Index
    leagueId: int,   // Index
    status: string,  // Index
    startTime: DateTime,
    marketCount: int,
    odds: [{ marketId: int, odds: float }],
    lastSyncTime: DateTime
  }
- Indexes: fixtureId, sportId, leagueId, status, startTime
- Sync: When OnUpdateFixtureMessage arrives in WebSocket.Service
- Invalidation: Delete when fixture ends or markets close
- Read Preference: PrimaryPreferred
- Expected Query Time: <50ms for indexed queries
```

---

## 5. SQL Agent

**Primary Responsibility:** Database design, EF Core configuration, query optimization

### Scope

- Entity design and relationships
- DbContext configuration
- Query optimization
- Database migration planning
- Index strategy
- Transaction management
- Multi-database coordination

### Constraints

- Four separate databases (Backoffice, Trading, Sportsbook, Translation)
- Must use Entity Framework Core (no raw SQL)
- Cannot change existing table schemas without migration
- 180-second query timeout must be respected
- Must use `AsNoTracking()` for read queries
- DbContext is scoped per HTTP request in API

### Workflow

1. **Data Model Analysis** - Understand entity relationships
2. **Context Mapping** - Determine which DbContext owns the entity
3. **Entity Design** - Define entity properties and relationships
4. **Query Analysis** - Review queries for N+1 and optimization
5. **Index Strategy** - Identify required database indexes
6. **Migration Planning** - Plan database migration if schema changes
7. **Performance Validation** - Ensure queries execute within timeout

### Expected Output

- Entity class with all properties and relationships
- DbContext configuration for entity
- Recommended database indexes
- Query optimization guidance (`AsNoTracking()`, `Include()`, etc.)
- Migration script (if needed) with roll-back strategy
- Performance baseline (expected query execution time)
- Code examples for common queries

### Example Usage

```
Request: "Add client region support to backoffice database"

Response:
- Entity: ClientInformation
- New Property:
  public string RegionCode { get; set; }  // 2-char code: HK, SG, etc.
- Relationship: None (client-region mapping exists separately)
- Context: BackofficeContext
- Migration:
  1. Add nullable column (backward compatible)
  2. Populate from configuration
  3. Add NOT NULL constraint
- Index: CREATE INDEX IX_ClientRegion ON ClientInformation(RegionCode)
- Query Optimization: 
  var clients = await _context.Clients
    .AsNoTracking()
    .Where(c => c.RegionCode == region)
    .ToListAsync();
- Expected Performance: <100ms for 10K clients
```

---

## 6. Translation Agent

**Primary Responsibility:** Multi-language support, translation data management

### Scope

- Translation entity design
- Language code standardization
- Fallback language strategy
- Translation API design
- Caching for translation data
- Language-specific content

### Constraints

- Translations stored in TranslationContext database
- Must support ISO 639-1 language codes with region variants
- Must implement fallback logic (language → base → default)
- Cannot accept null translation responses
- Caching must respect language-specific TTL

### Workflow

1. **Language Requirement** - Identify what content needs translation
2. **Entity Design** - Design translation entity for content
3. **Language Support** - List supported languages and regions
4. **Fallback Strategy** - Determine fallback chain (e.g., en-US → en → default)
5. **API Design** - Design translation retrieval endpoints
6. **Caching Strategy** - Plan caching for frequently accessed translations
7. **Validation** - Ensure all translatable content has translations

### Expected Output

- Translation entity schema
- Supported languages list
- Fallback chain definition
- API endpoint for getting translations
- Caching strategy (TTL per language)
- Code for translation retrieval with fallback
- Database query to populate base translations
- Language-specific performance considerations

### Example Usage

```
Request: "Add translations for market names"

Response:
- Entity: MarketTranslation
- Fields:
  - MarketId (FK)
  - LanguageCode (e.g., "en", "es", "en-US")
  - Name (translated market name)
- Supported Languages: en, es, fr, de, zh-CN, pt-BR
- Fallback Chain: 
  1. Try exact language (en-US)
  2. Try base language (en)
  3. Use English (en)
  4. Use market code as fallback
- Query:
  var translation = await GetMarketTranslation(marketId, "es");
  // Returns: MarketTranslation or Name from code
- Caching: 24-hour TTL per language
- Index: (MarketId, LanguageCode)
```

---

## 7. Cache Agent

**Primary Responsibility:** Caching strategy, performance optimization, cache invalidation

### Scope

- Cache key design
- TTL (time-to-live) strategy
- Lazy-load patterns
- Cache invalidation propagation
- Cache warming strategies
- Performance monitoring

### Constraints

- Uses System.Runtime.Caching.MemoryCache (in-process)
- Cache not distributed across servers
- TTL in hours or absolute DateTimeOffset
- Must propagate invalidation via RabbitMQ `OnDeleteCacheMessage`
- Cannot cache user-specific or sensitive data indefinitely

### Workflow

1. **Cache Candidate Analysis** - Identify if data is cacheable
2. **TTL Strategy** - Determine appropriate cache timeout
3. **Key Design** - Design hierarchical cache keys
4. **Lazy-Load Plan** - Determine fallback function for cache miss
5. **Invalidation Rules** - Identify when cache should be cleared
6. **Distribution Plan** - Plan cache invalidation broadcast
7. **Testing** - Ensure cache hits/misses are properly tested

### Expected Output

- Cache key naming convention for the feature
- Recommended TTL (in hours or datetime)
- Lazy-load function pattern
- Invalidation trigger points
- RabbitMQ message definition for cache invalidation
- Code template for cache get/set
- Performance impact (cache hit rate expectation)
- Cache warming strategy (if applicable)

### Example Usage

```
Request: "Cache client configuration to improve API performance"

Response:
- Cache Key: "config.client.{clientId}"
- TTL: 24 hours (config rarely changes)
- Lazy-Load:
  var config = _memCache.GetCache<ClientConfig>(
    "config.client.123",
    () => _context.ClientConfigs.FirstOrDefault(c => c.ClientId == 123),
    expiration: 24
  );
- Invalidation Triggers:
  1. OnUpdateClientMarketConfigMessage received
  2. OnUpdateClientLeagueConfigMessage received
  3. OnUpdateClientSettingsMessage received
- Broadcast:
  await _messageBroker.Publish(new OnDeleteCacheMessage
  {
    CacheKey = "config.client.123",
    ClientId = 123
  });
- Hit Rate Expectation: >80% (same users access same clients)
```

---

## 8. Messaging Agent

**Primary Responsibility:** Message type design, RabbitMQ flow, event handling

### Scope

- Message type definition
- Publishing strategy (when to publish)
- Consumer identification (who consumes)
- Routing key design
- Message retry/reliability
- Fire-and-forget patterns
- Message versioning

### Constraints

- RabbitMQ topic exchange with pattern-based routing
- Messages published via IRabbitMqRepository using HTTP API
- Routing key format: `r.oe.{topic}.{messagetype}`
- Consumers in Config.Service, Utility, WebSocket.Service only
- Fire-and-forget pattern (no guaranteed delivery)
- Serialization via System.Text.Json

### Workflow

1. **Event Definition** - Identify what event needs to be published
2. **Message Type** - Design message class with required fields
3. **Publisher Identification** - Determine which service publishes
4. **Consumer Mapping** - Identify all consuming services
5. **Routing Strategy** - Design routing key for message routing
6. **Payload Design** - Structure message payload
7. **Idempotency** - Plan for handling duplicate messages
8. **Documentation** - Document message flow in 06_Messaging.md

### Expected Output

- Message type class definition
- Publishing code pattern
- Routing key specification
- Consumer handler implementation
- Message format documentation
- Deployment sequence for producers/consumers
- Considerations for message reliability
- Example message payload
- Documentation updates to 06_Messaging.md

### Example Usage

```
Request: "Add message for client budget limit changes"

Response:
- Message Type: OnClientBudgetLimitChangeMessage
- Fields:
  {
    ClientId: int,
    BudgetLimit: decimal,
    Currency: string,
    EffectiveDate: DateTime,
    ChangedBy: string
  }
- Publisher: BackofficeRepository.UpdateClientBudgetLimit()
- Publishing Code:
  await _messageBroker.Publish(new OnClientBudgetLimitChangeMessage
  {
    ClientId = clientId,
    BudgetLimit = newLimit,
    Currency = "USD",
    EffectiveDate = DateTime.UtcNow,
    ChangedBy = userId
  }, region: "HK");

- Consumers:
  - Utility Service: Log budget change
  - WebSocket Service: Notify connected clients
  - Config Service: Update client's cached budget

- Routing Key: r.oe.client.onclientbudgetlimitchange
- CorrelationId: Auto-generated GUID (uppercase)
- Reliability: Fire-and-forget (log failures but don't retry)
- Duplicate Handling: Idempotent by design (updates are idempotent)
```

---

## 9. Performance Agent

**Primary Responsibility:** Performance optimization, bottleneck identification, benchmarking

### Scope

- Database query optimization
- N+1 query detection
- Caching strategy for performance
- HttpClient pooling
- Async/await usage
- Memory leak detection
- Query execution time analysis

### Constraints

- 180-second query timeout must be respected
- Must use `AsNoTracking()` for read-heavy operations
- Must use connection pooling for databases
- Cannot introduce blocking operations in message handlers
- Performance changes must be backward compatible

### Workflow

1. **Bottleneck Identification** - Analyze slow operations
2. **Query Analysis** - Review database queries for optimization
3. **Cache Audit** - Identify caching opportunities
4. **Connection Analysis** - Check connection pooling configuration
5. **Async Review** - Verify async/await patterns
6. **Benchmark Planning** - Design performance tests
7. **Optimization Strategy** - Recommend specific improvements
8. **Validation** - Ensure performance improvements verified

### Expected Output

- Performance bottleneck analysis
- Database query optimization recommendations
- Caching opportunities with ROI estimates
- Connection pooling configuration review
- Async pattern recommendations
- Benchmark test plan
- Expected performance improvement (%)
- Rollback strategy if optimization causes issues

### Example Usage

```
Request: "Optimize slow client list retrieval"

Response:
- Current Performance: 5 seconds for 10K clients
- Bottleneck Analysis:
  - Query not using AsNoTracking()
  - Missing index on Status column
  - Client objects being translated to DTO in-memory
- Optimizations:
  1. Add .AsNoTracking() to query
  2. Create index: IX_ClientStatus ON Clients(Status)
  3. Use database projection instead of in-memory mapping
  4. Add 1-hour MemoryCache with key "clients.list.all"
- Expected Results:
  - Query time: 5s → 100ms (50x improvement)
  - Cache hits: 100ms → <5ms
- Benchmark Plan: Load test with 100 concurrent requests
- Rollback: Revert index and remove caching
```

---

## 10. Code Review Agent

**Primary Responsibility:** Code quality assessment, compliance checking, best practices verification

### Scope

- SOLID principle compliance
- Coding conventions adherence
- Security vulnerabilities
- Performance concerns
- Error handling adequacy
- Testing coverage
- Documentation completeness
- Backward compatibility

### Constraints

- Must validate against CLAUDE.md conventions
- Cannot approve breaking changes
- Cannot approve unlogged errors
- Cannot approve queries without proper EF patterns
- Cannot approve unhandled exceptions
- Must verify Safety & Environment Rules compliance: no Production-modifying code without explicit approval marker, no direct SQL/MongoDB writes outside the repository pattern, no connection strings in committed code, no untestable code structure
- Must flag any commit that spans multiple unrelated domains (focused commit rule violation)
- Must verify that relevant `docs/*.md` files are updated as part of the change

### Workflow

1. **Code Submission** - Review provided code
2. **Convention Check** - Verify against CLAUDE.md standards
3. **Architecture Validation** - Check against established patterns
4. **Security Audit** - Look for security issues
5. **Performance Review** - Identify performance concerns
6. **Error Handling** - Verify proper exception handling
7. **Testing Assessment** - Evaluate test adequacy
8. **Feedback Report** - Provide structured feedback

### Expected Output

- Overall approval status (approved, changes requested, reject)
- Section-by-section feedback
- Identified issues categorized by severity (critical, high, medium, low)
- Specific recommendations for improvements
- Code examples showing recommended patterns
- References to CLAUDE.md sections
- Testing recommendations
- Performance considerations

### Example Usage

```
Request: "Review code changes for client budget update feature"

Feedback:
✓ Architecture: Follows repository pattern correctly
✓ Database: Proper use of DbContext, AsNoTracking()
✓ Caching: Cache invalidation properly broadcast via RabbitMQ
✗ Error Handling: CRITICAL - Null reference possible on region header
✗ Performance: HIGH - Multiple HttpClient creations, should be pooled
✗ Testing: MEDIUM - No unit tests for edge cases
⚠ Documentation: Update 06_Messaging.md with new message type

Recommendations:
1. Add null-coalescing for region header
2. Inject IHttpClientFactory instead of creating HttpClient
3. Add unit tests for budget validation
4. Update CLAUDE.md example if pattern differs
5. Add activity logging for budget changes

Status: Changes Requested (resubmit after fixes)
```

---

## Agent Invocation Guide

### How to Invoke Agents

When working on tasks in VS Code, reference agents by name:

```
"@Architecture Agent - Assess impact of adding real-time notifications"
"@Feature Implementation Agent - Implement client preference sync"
"@API Agent - Design endpoint for client region management"
"@MongoDB Agent - Design Fixture cache collection"
"@SQL Agent - Add RegionCode to ClientInformation table"
"@Translation Agent - Add translations for new market types"
"@Cache Agent - Optimize fixture lookup caching"
"@Messaging Agent - Create OnClientStatusChangeMessage"
"@Performance Agent - Optimize slow client report queries"
"@Code Review Agent - Review client budget implementation"
```

### Agent Sequencing

For complex features, invoke agents in this order:

1. **Architecture Agent** → Validate design across 7 projects
2. **Feature Implementation Agent** → Plan step-by-step implementation
3. **SQL Agent** / **MongoDB Agent** → Design data layer
4. **API Agent** → Design endpoints
5. **Messaging Agent** → Plan RabbitMQ flow
6. **Cache Agent** → Optimize performance
7. **Code Review Agent** → Final quality check

### Agent Interaction

Agents can reference each other:

- Architecture Agent may recommend Messaging Agent involvement
- Feature Implementation Agent may reference Cache Agent patterns
- Code Review Agent verifies recommendations from all agents

---

## Agent Decision Matrix

Use this matrix to determine which agent(s) to involve:

| Task | Agent(s) |
|------|----------|
| Design new API endpoint | API Agent |
| Optimize slow database query | SQL Agent, Performance Agent |
| Add real-time feature | Architecture Agent, Messaging Agent, WebSocket considerations |
| Cache performance issue | Cache Agent, Performance Agent |
| Multi-language feature | Translation Agent, SQL Agent |
| MongoDB query issue | MongoDB Agent, Performance Agent |
| Message type design | Messaging Agent, Architecture Agent |
| Code quality review | Code Review Agent |
| Feature specification | Architecture Agent, Feature Implementation Agent |
| Bug analysis | Code Review Agent, Performance Agent (if performance-related) |
| Cross-service integration | Architecture Agent, Messaging Agent |

---

## Agent Responsibilities Matrix

| Task | Arch | Feature | API | Mongo | SQL | Trans | Cache | Msg | Perf | Review |
|------|------|---------|-----|-------|-----|-------|-------|-----|------|--------|
| New endpoint | ✓ | | ✓ | | | | | | | ✓ |
| Data model | | ✓ | | ✓ | ✓ | | | | | |
| Performance | ✓ | | | | ✓ | | ✓ | | ✓ | |
| Messaging | ✓ | | | | | | | ✓ | | |
| Translation | | | | | | ✓ | ✓ | | | |
| Code review | | | | | | | | | | ✓ |
| Architecture | ✓ | ✓ | | | | | | | | |

---

## When NOT to Use Agents

- **Simple bug fixes** - Direct code editing may be faster
- **Small refactors** - If change is localized, implement directly
- **Documentation updates** - Write directly unless validation needed
- **Configuration changes** - Update appsettings.json directly
- **Trivial code changes** - Less than 10 lines of simple code

---

## Combining Agents for Complex Tasks

### Example: "Implement Client Tier System"

**Sequence:**

1. **Architecture Agent**
   - Assess impact on all 7 projects
   - Identify integration points
   - Plan deployment sequence

2. **SQL Agent**
   - Add TierLevel column to ClientInformation
   - Create ClientTier lookup table
   - Design index strategy

3. **API Agent**
   - Design endpoints for tier management
   - Plan tier change operation

4. **Messaging Agent**
   - Create OnClientTierChangeMessage
   - Identify consumers (WebSocket, Config.Service)

5. **Cache Agent**
   - Optimize tier lookups
   - Plan cache invalidation

6. **Performance Agent**
   - Benchmark tier lookup queries
   - Identify optimization opportunities

7. **Code Review Agent**
   - Final quality assessment
   - Verify cross-agent recommendations

---

## Agent Output Standards

All agents produce structured output with:

1. **Executive Summary** - Brief overview of recommendation
2. **Detailed Analysis** - Thorough explanation with reasoning
3. **Code Examples** - Specific code templates when applicable
4. **Implementation Steps** - Numbered steps to follow
5. **Testing Strategy** - How to validate implementation
6. **Deployment Considerations** - Release notes, dependencies
7. **Related Documentation** - References to CLAUDE.md sections
8. **Success Criteria** - How to verify successful implementation

---

## Escalation Path

If agent recommendation conflicts with production stability:

1. **Code Review Agent** must validate any risky changes
2. **Architecture Agent** must assess impact on multiple services
3. **Performance Agent** must ensure no performance regression
4. All concerns must be documented in pull request before deployment

---

## Continuous Improvement

As codebase evolves:
- Update agent descriptions to reflect new patterns
- Add new agents if new development areas emerge
- Deprecate agents if approaches change
- Keep CLAUDE.md and agent definitions synchronized

---

**Last Updated:** June 2026 (Universal Safety Constraints added)  
**Next Review:** December 2026 (post-.NET upgrade if planned)
