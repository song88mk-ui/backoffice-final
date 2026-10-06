---
name: feature-implementation
description: Step-by-step workflow for implementing a new feature in the Backoffice solution — data layer, service layer, API layer, WebSocket/real-time, background processing, testing, and documentation. Use when adding new functionality to any of the 7 Backoffice.* projects.
---

## Feature Implementation Workflow

### Step 1: Define the Feature

- Identify which project(s) are affected
- Determine if new database table is needed
- Plan message flow if async component involved

### Step 2: Data Layer

1. Add entity to appropriate DbContext if needed
2. Implement repository method following convention
3. Schema changes: this repo has no EF migrations. The databases are shared with other repos, so agree any schema change with them first
4. Test directly against database

### Step 3: Service Layer

1. Create service class following naming conventions
2. Inject repository dependencies
3. Implement business logic
4. Add caching if needed

### Step 4: API Layer

1. Add controller or extend existing
2. Create request/response DTOs
3. Implement endpoint with proper authorization
4. Add Swagger documentation

### Step 5: WebSocket/Real-Time (if needed)

1. Add SignalR Hub method if real-time updates needed
2. Add message type to OutgoingEngineMessages
3. Update WebSocket.Service message handler

### Step 6: Background Processing (if needed)

1. Define message type in Shared
2. Add publishing in API layer
3. Add consuming logic in appropriate background service
4. Add activity logging

### Step 7: Testing

Follow the "Testing requirements (mandatory)" section in CLAUDE.md: add unit tests and report Build Status, Unit Tests, Integration Tests, Manual Verification and Remaining Risks. Also:

1. Test API endpoint manually
2. Verify message publishing/consumption
3. Check cache invalidation
4. Verify cross-service integration

### Step 8: Documentation

1. Update 02_ProjectOverview.md if adding new project
2. Update 06_Messaging.md if adding message type
3. Add code comments for complex logic
