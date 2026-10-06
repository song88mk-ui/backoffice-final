---
name: code-review-checklist
description: Pre-submission checklist for Backoffice code changes covering architecture/design, database/performance, error handling/logging, messaging/events, security/validation, and documentation/testing. Use before submitting or requesting review of a change in this repo.
---

## Code Review Checklist

Before submitting code for review, verify:

### Architecture & Design
- [ ] Follows established repository pattern
- [ ] Uses dependency injection (no `new` keyword for services)
- [ ] Maintains single responsibility
- [ ] Backward compatible (no breaking API changes)

### Database & Performance
- [ ] Uses `AsNoTracking()` for read queries
- [ ] Includes `.Include()` for navigation properties
- [ ] Uses appropriate caching (or explains why not)
- [ ] No N+1 query patterns observed
- [ ] Query timeout respected (180s)

### Error Handling & Logging
- [ ] All repository methods wrapped in try-catch
- [ ] Returns `BaseResponse` with appropriate `StatusCode`
- [ ] Meaningful error logged with context
- [ ] No sensitive data logged

### Messaging & Events
- [ ] Message types defined in Backoffice.Shared
- [ ] Publishing includes `CorrelationId`
- [ ] Consumer handles all expected message types
- [ ] Cache invalidation cascaded via RabbitMQ

### Security & Validation
- [ ] API endpoints have the appropriate `[Authorize]` attribute or `ServiceFilter(typeof(ApiKeyFilter))`
- [ ] Request models validated with data annotations
- [ ] No SQL injection vectors (EF Core usage)
- [ ] API key and JWT logic preserved

### Documentation & Testing
- [ ] Complex logic commented
- [ ] New message types documented
- [ ] Tested across all affected services
- [ ] No compiler warnings
