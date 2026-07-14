# Potential Bugs Identified

**Document Status:** Analysis Complete (No Code Changes Made)  
**Date:** June 18, 2026

---

## Null Reference Exceptions

### Bug #1: NullReferenceException in AuthorizeAttribute

**Location:** `Backoffice.API/ActionFilter/AuthorizeAttribute.cs`

**Code:**
```csharp
var region = context.HttpContext.Request.Headers["x-region"];  // May be null
// Used without null check later
```

**Risk:** ⚠️ HIGH

**Scenario:**
```
1. Client sends request without x-region header
2. region = null
3. Later code uses region → NullReferenceException
```

**Expected Behavior:**
```csharp
var region = context.HttpContext.Request.Headers["x-region"]?.ToString() ?? "default";
```

---

### Bug #2: Bearer Token Parsing

**Location:** `Backoffice.API/ActionFilter/AuthorizeAttribute.cs`

**Code:**
```csharp
var token = context.HttpContext.Request.Headers["Authorization"]
    .ToString()
    .Replace("Bearer ", "");  // If no "Bearer " prefix, whole string kept as token
```

**Risk:** ⚠️ MEDIUM

**Scenario:**
```
1. Client sends: Authorization: InvalidToken
2. Replace doesn't find "Bearer " → token = "InvalidToken"
3. Token validation fails (correctly)
4. But error message misleading (suggests malformed header)
```

**Expected Behavior:**
```csharp
var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
if (!authHeader.StartsWith("Bearer "))
{
    // Return 401: Invalid header format
}
var token = authHeader.Substring("Bearer ".Length);
```

---

### Bug #3: Null Response Content

**Location:** `Backoffice.API/Repositories/PlatformRepository.cs`

**Code:**
```csharp
var response = await _httpClient.PostAsync(uri, content);
var result = JsonConvert.DeserializeObject<BaseResponse<T>>(
    await response.Content.ReadAsStringAsync());  // May be null/empty
return result.Result;  // NullReferenceException if result is null
```

**Risk:** ⚠️ HIGH

**Scenario:**
```
1. HTTP 500 from platform API
2. response.Content = empty
3. JsonConvert.DeserializeObject returns null
4. result.Result throws NullReferenceException
```

---

## Async/Await Issues

### Bug #4: Fire-and-Forget Task (OnAuthorization)

**Location:** `Backoffice.API/ActionFilter/AuthorizeAttribute.cs`

**Code:**
```csharp
public async void OnAuthorization(AuthorizationFilterContext context)  // WRONG
{
    // async method as void
    var response = await ValidateTokenAsync();
    // If exception occurs, it's unobserved
}
```

**Risk:** ⚠️ CRITICAL

**Issue:**
- Exceptions in async void methods are thrown on synchronization context
- Can crash the whole application
- Hard to debug

**Expected:**
```csharp
public void OnAuthorization(AuthorizationFilterContext context)
{
    // Synchronous method
    // OR call async properly:
}

// Better approach
public class AuthorizeAttribute : AuthorizeAsyncAttribute
{
    public override async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Proper async handling
    }
}
```

---

### Bug #5: Sync Over Async in Config.Service

**Location:** `Backoffice.Config.Service/Worker.cs`

**Code:**
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        var message = await _consumer.ReceiveAsync(timeout: TimeSpan.FromSeconds(5));
        
        // Blocks thread waiting for database
        _repository.ProcessMessage(message);  // Not awaited!
    }
}
```

**Risk:** ⚠️ MEDIUM

**Issue:**
- Synchronous database call blocks async loop
- Thread pool starvation possible
- Defeats purpose of async

---

## Database Concurrency

### Bug #6: Race Condition in DbContext Initialization

**Location:** `Backoffice.API/Repositories/SportsbookRepository.cs`

**Code:**
```csharp
private static IMongoDatabase _db;

public SportsbookRepository(...)
{
    if (_db == null)  // Not thread-safe check
    {
        var client = new MongoClient(connectionString);
        _db = client.GetDatabase("Sportsbook");  // Multiple threads could execute this
    }
}
```

**Risk:** ⚠️ HIGH

**Scenario:**
```
1. Thread 1 checks: _db == null (true)
2. Thread 2 checks: _db == null (true)
3. Both create MongoClient
4. Both set _db (race condition)
5. Unpredictable behavior
```

**Expected:**
```csharp
private static readonly Lazy<IMongoDatabase> _db = 
    new Lazy<IMongoDatabase>(() => 
    {
        var client = new MongoClient(connectionString);
        return client.GetDatabase("Sportsbook");
    });
```

---

### Bug #7: DbContext Without Using Statement

**Location:** `Backoffice.Config.Service/Repository/BackofficeRepository.cs`

**Code:**
```csharp
public BackofficeRepository(IConfiguration configuration)
{
    var optionsBuilder = new DbContextOptionsBuilder<BackofficeContext>();
    optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:Backoffice"]);
    
    _backofficeContext = new BackofficeContext(optionsBuilder.Options);
    // Never disposed! Long-running background service
}
```

**Risk:** ⚠️ HIGH

**Issue:**
- DbContext never disposed
- Connection never returned to pool
- Memory leak in long-running process
- Each message creates new unused DbContext

**Expected:**
```csharp
using (var context = new BackofficeContext(options))
{
    // Do work
} // Context automatically disposed
```

---

## Message Handling

### Bug #8: Unhandled Exception in Message Consumer

**Location:** `Backoffice.Config.Service/Worker.cs`

**Code:**
```csharp
try
{
    switch (messageType)
    {
        case "DeleteConfigTemplate":
            var template = JsonSerializer.Deserialize<ConfigTemplate>(payload);
            _repository.DeleteConfigTemplate(template);
            // What if Deserialize throws? What if Delete throws?
            break;
    }
}
catch (Exception ex)
{
    _logger.LogError("Error: {ex}", ex);
    // Message silently nacked and requeued
}
```

**Risk:** ⚠️ MEDIUM

**Issue:**
- If message processing fails, it's requeued
- If always fails, it loops infinitely
- No dead letter queue fallback
- No max retry limit

---

### Bug #9: Fire-and-Forget Message Publishing

**Location:** `Backoffice.API/Repositories/*.cs` (all repositories)

**Code:**
```csharp
await _rabbitMqRepository.Publish(messageRequest, region);
// No error handling if publish fails
// Database updated but message never sent
// Inconsistent state
```

**Risk:** ⚠️ CRITICAL

**Scenario:**
```
1. API updates client configuration
2. Database write succeeds
3. Message publish fails (network issue)
4. Cache invalidation message never sent
5. Clients see stale config
```

---

## Configuration Issues

### Bug #10: Missing Configuration Falls Through to Null

**Location:** `Backoffice.API/Startup.cs`

**Code:**
```csharp
var dbConnectionString = configuration["ConnectionStrings:Backoffice"];
// If key missing, returns null instead of throwing

services.AddDbContext<BackofficeContext>(options =>
    options.UseSqlServer(dbConnectionString));  // null connection string!
```

**Risk:** ⚠️ HIGH

**Scenario:**
```
1. Configuration missing ConnectionStrings:Backoffice
2. dbConnectionString = null
3. Service starts (no error)
4. First database query: NullReferenceException
```

---

## Type Conversion Issues

### Bug #11: Implicit String to Long Conversion

**Location:** `Backoffice.API/Repositories/RabbitMQRepository.cs`

**Code:**
```csharp
var value = JsonSerializer.Deserialize<object>(payload);
var clientId = (long)value;  // If string, throws InvalidCastException
```

**Risk:** ⚠️ MEDIUM

**Scenario:**
```
1. Message payload: "clientId": "123" (string)
2. Deserialize as object → object = "123"
3. (long) cast fails
4. Unhandled exception
```

---

## HTTP Client Issues

### Bug #12: HttpClient Not Disposed

**Location:** `Backoffice.API/Repositories/RabbitMQRepository.cs`

**Code:**
```csharp
private HttpClient GetHttpClient(string region)
{
    var client = new HttpClient();  // New instance every call
    return client;  // Never disposed
}

// Usage
var client = GetHttpClient("HK");
await client.PostAsync(url, content);  // HttpClient leaked
```

**Risk:** ⚠️ CRITICAL

**Issue:**
- Socket exhaustion: No sockets available
- Memory leak: HttpClient holds resources
- Performance degradation: Creating HttpClient is expensive

**Observed in:**
- RabbitMQRepository
- Multiple repository HTTP calls

---

## Serialization Issues

### Bug #13: DateTime Format Mismatch

**Location:** `Backoffice.API/Controllers/*.cs` and Repository files

**Issue:** Mixed JSON serializers

```csharp
// System.Text.Json (ISO 8601)
JsonSerializer.Serialize(new { date = DateTime.Now });
// Output: "date": "2026-06-18T14:30:45.123Z"

vs

// Newtonsoft.Json (configurable)
JsonConvert.SerializeObject(new { date = DateTime.Now });
// Output: "date": "06/18/2026 14:30:45" (depends on settings)
```

**Risk:** ⚠️ MEDIUM

**Scenario:**
```
1. API publishes message using System.Text.Json
2. Consumer deserializes using Newtonsoft.Json
3. DateTime parsing fails
4. Message processing fails
```

---

## Business Logic Issues

### Bug #14: Activity Log Processing

**Location:** `Backoffice.API/Repositories/BackOfficeRepository.cs`

**Code:**
```csharp
// Note: Will Remove
public async Task<BaseResponse<bool>> AddActivityLog(ActivityLogModel request)
{
    // Still called from controllers
    // Unclear if should remove or keep
}
```

**Risk:** ⚠️ MEDIUM

**Issue:**
- Function marked for removal but still called
- If removed, breaks callers
- If kept, increases technical debt
- Deprecation unclear

---

### Bug #15: Configuration Template Validation

**Location:** `Backoffice.API/Repositories/BackOfficeRepository.cs`

**Code:**
```csharp
public async Task<BaseResponse<ConfigTemplate>> AddConfigTemplate(ConfigTemplate template)
{
    // No validation of template properties
    // Could add null properties
    // Could add invalid values
    
    await _context.ConfigTemplate.AddAsync(template);
    await _context.SaveChangesAsync();
    
    return new BaseResponse { Result = template };
}
```

**Risk:** ⚠️ MEDIUM

**Expected:**
```csharp
if (template == null || string.IsNullOrEmpty(template.Name))
    return new BaseResponse { StatusCode = StatusCode.ValidationError };
```

---

## Potential Runtime Errors Summary

| Bug # | Issue | Type | Severity | Impact |
|-------|-------|------|----------|--------|
| 1 | Null region header | NullRef | HIGH | 400 Bad Request |
| 2 | Bearer parsing | Logic | MEDIUM | Wrong token |
| 3 | Null response | NullRef | HIGH | 500 Error |
| 4 | async void | Design | CRITICAL | App crash |
| 5 | Sync over async | Design | MEDIUM | Thread starvation |
| 6 | Race condition MongoDB | Threading | HIGH | Unpredictable state |
| 7 | DbContext leak | Resource | HIGH | Connection pool |
| 8 | Unhandled message error | Exception | MEDIUM | Infinite loop |
| 9 | Fire & forget publish | Reliability | CRITICAL | Stale cache |
| 10 | Null config | Config | HIGH | Runtime error |
| 11 | Type conversion | Serialization | MEDIUM | Parse error |
| 12 | HttpClient leak | Resource | CRITICAL | Socket exhaustion |
| 13 | DateTime format | Serialization | MEDIUM | Parse error |
| 14 | Ambiguous deprecation | Code Quality | MEDIUM | Maintenance |
| 15 | No input validation | Logic | MEDIUM | Invalid data |

---

## Testing Recommendations

### Unit Tests Needed

- [ ] ApiKeyFilter with missing header
- [ ] Bearer token parsing with invalid format
- [ ] Repository with null responses
- [ ] Message deserialization with wrong types
- [ ] Configuration validation on startup

### Integration Tests Needed

- [ ] End-to-end authentication flow
- [ ] Message publishing and consumption
- [ ] DbContext lifecycle in background worker
- [ ] HttpClient reuse and cleanup

### Load Tests Needed

- [ ] Socket exhaustion with HttpClient leak
- [ ] Thread safety of MongoDB connection
- [ ] Message consumer throughput and backlog
- [ ] Memory usage over time

