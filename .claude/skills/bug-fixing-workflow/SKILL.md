---
name: bug-fixing-workflow
description: Step-by-step workflow for fixing a bug in the Backoffice solution — reproduce, identify affected services, implement the fix, cascade-test across consuming services, and review deployment considerations. Use when triaging or fixing a defect in any of the 7 Backoffice.* projects.
---

## Bug Fixing Workflow

### Step 1: Reproduce

1. Identify which service exhibits the bug
2. Determine if it's API, background service, or WebSocket
3. Create minimal reproducible test case
4. Understand root cause

### Step 2: Identify Affected Services

Check if bug touches:
- Database operations → May need a schema change in a shared database (no EF migrations in this repo)
- Message handling → May need message handler update
- Cache operations → May need cache invalidation
- External API calls → May need fallback logic

### Step 3: Fix Implementation

1. Implement minimal fix in responsible repository/service
2. Add error handling if not present
3. Add logging for troubleshooting
4. Verify backward compatibility

### Step 4: Cascade Testing

1. Test in originating service
2. Test in consuming services
3. Verify cache invalidation propagates
4. Verify message handling works correctly

### Step 5: Deployment Considerations

1. Does fix require a schema change in a shared database?
2. Does fix require configuration change?
3. Is backwards compatibility maintained?
4. Are there dependent services to update?
