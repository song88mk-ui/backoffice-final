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

## Maintenance Mode UI (Fixed)

### Bug #16: Checking a client didn't enable maintenance for it (BO-115)

**Location:** `Backoffice.Web/Pages/Administration/MaintenanceV2.razor.cs`

**Issue:** the "Enable Maintenance Mode" master switch (`_isMaintenance`) and the "Specific
Clients" checklist were independent state. Checking a client only marked it as the target of
whatever the switch already held; nothing set the switch when you checked a client that wasn't
already under maintenance. An operator checking a live client and clicking Save got a silent
`IsMaintenance:false` save (toast: "Disabling Maintenance") instead of the expected enable, with
no error. Confirmed against Staging's `ActivityLog2` and bo-api logs: the requests sent during
reproduction were `{"ClientIds":"AL:...","IsMaintenance":false}` even though the operator intended
to enable maintenance for that client.

**Risk:** ⚠️ HIGH (operational: maintenance mode appears to do nothing when toggled this way)

**Fix:** `HandlePropertyChange` now sets `_isMaintenance = true` whenever the checked-client set
includes a client not already under maintenance, mirroring the pattern `HandleChangeMode` already
used for the initial mode-switch case. The switch stays manually overridable.

**Tests:** `Backoffice.Web.Tests/Administration/MaintenanceV2PageTests.cs`

---

### Bug #17: Client search box in the maintenance dropdown never filtered

**Location:** `Backoffice.Web/Components/Administration/MaintenanceV2/ClientSelectionDropDown.razor(.cs)`

**Issue:** the search `<input>` used plain `@bind="@_searchField"` with no handler wired to it
(`HandleSearchInput()` existed but was an empty, unused stub). The actual `.Where(...)` filter only
ran inside `OnParametersSet()`, gated by `ParameterChanged()`, which only detects changes to the
checked-item set or the `Disabled` parameter, not to the search text. Typing in the box updated the
backing field but never re-rendered or re-filtered the list; with ~200 near-duplicate per-layout
entries, this made finding the correct client effectively impossible. As a secondary effect, the
old code also reassigned the `Clients` parameter itself to the filtered subset and pushed it back to
the parent via `ClientsChanged`, so a filter that did apply (indirectly, via a checkbox click) could
permanently shrink the parent's full client list.

**Risk:** ⚠️ MEDIUM (usability; compounds Bug #16 by making the correct row hard to find)

**Fix:** the search input now uses `@oninput` wired to a real handler that updates `_searchField`
and requests a render; the list renders from a non-mutating `FilteredClients()` projection instead
of overwriting the `Clients` parameter.

**Tests:** `Backoffice.Web.Tests/Administration/ClientSelectionDropDownTests.cs`

---

## Activity Log Search (Fixed)

### Bug #18: Client Site Config logs search never matched a client (BO-104)

**Location:** `Backoffice.API/Repositories/BackOfficeRepository.cs` (`GetActivityLogs` →
`FilterActivityLogsBySearch`), `Backoffice.Web/Components/Administration/ClientSiteConfig/ClientSiteConfigLogsModal.razor.cs`

**Issue:** the shared View Logs search filter only matched `From`, `To` and `UserName`. A Client
Site Config log row stores the client only as a GUID in `Payload` (`"{userName}.{clientGuid}"`);
the modal's "Client" column ("Name (Code)") is resolved in the browser from that GUID. Searching a
client name or code therefore matched nothing and showed "No Record Found". The first BO-104 fix
(#188) only made the search icon submit the form. Separately, the filter called `.ToUpper()` on
each field, so a single row with a null `From`/`To`/`UserName` threw into the repository's
catch-all and every View Logs modal rendered the error as "No Record Found".

**Risk:** ⚠️ MEDIUM (usability: the search couldn't find a specific client's changes)

**Fix:** `ActivityLogRequest.PayloadSearchTerms` (additive, defaults to empty). The modal resolves
the search text to the GUIDs of clients whose name or code contains it, and the filter also
matches those against `Payload`. Raw `SearchText` is still never matched against `Payload`, so
other modals behave as before. The filter is now null-safe and case-insensitive via
`OrdinalIgnoreCase`.

**Expected behaviour:** name and code are each matched on any substring, case-insensitively. For
client name `GIMLI` / code `G02`, all of `IM`, `GIM`, `LI`, `IMLI`, `GIMLI`, `02`, `G02` (and their
lower/mixed-case forms) return that client's logs.

**Tests:** `Backoffice.API.Tests/ActivityLog/ActivityLogSearchFilterTests.cs`,
`Backoffice.Web.Tests/Administration/ClientSiteConfigLogsModalTests.cs`
(`Search_ByPartialClientNameOrCode_IsCaseInsensitive` covers the examples above)

---

## Market Settlement Cancel Confirmation (Fixed)

### Bug #19: Discard prompt's Cancel/Confirm buttons were invisible (BO-102)

**Location:** `Backoffice.Web/Components/Administration/MarketSettlement/AddMarketSettlementModal.razor(.css/.cs)`

**Issue:** BO-18 added an inline "You have unsaved changes" prompt to the Add Market Settlement
modal, with `btn modal-btn modal-close` / `modal-apply` buttons. The component's scoped CSS sets
`.modal-btn { color: white; }` but never defined `.modal-close` / `.modal-apply`. Those classes
only exist in other components' scoped CSS, which does not apply here. The prompt therefore
showed its text with white-on-white buttons, so after pressing Cancel there seemed to be no way
to confirm or back out. The first BO-102 fix (#187) only hid the header X and did not address
this. QA reopened the ticket on 2026-10-13. Separately, the footer Cancel / Settle Market buttons
stayed active while the prompt was open.

**Risk:** ⚠️ LOW (usability; no data impact)

**Fix:** added `.modal-close` / `.modal-apply` rules to `AddMarketSettlementModal.razor.css`,
using the same values as `ConfirmCancelBetModal.razor.css`. The footer buttons now bind to
`IsFooterDisabled` (`IsPreLoader || ShowDiscardConfirm`).

**Gotcha:** Blazor CSS isolation is per component, so a class styled in one component's
`.razor.css` is unstyled in any other component that uses it.

**Tests:** `Backoffice.Web.Tests/Administration/MarketSettlementCancelConfirmationTests.cs`

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

