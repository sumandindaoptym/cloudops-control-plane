# CloudOps Control Plane - Architecture

## System Overview

The CloudOps Control Plane is an enterprise developer platform built as a monorepo with ASP.NET Core 9 backend and Next.js frontend, designed for queue-based task orchestration with real-time updates.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     CLIENT (Browser)                         │
│  ┌────────────────────────────────────────────────────┐    │
│  │            Next.js Frontend (Port 5000)             │    │
│  │  - Dashboard UI                                     │    │
│  │  - Real-time task monitoring (SignalR client)       │    │
│  │  - Action buttons (Deploy, Backup, Restore, etc.)   │    │
│  └────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ HTTP/SignalR
                           ▼
┌─────────────────────────────────────────────────────────────┐
│         ASP.NET Core API Process (Port 5056)                │
│                                                              │
│  ┌──────────────────────────────────────────────────┐      │
│  │              API Endpoints (Minimal APIs)         │      │
│  │  - POST /api/deployments                          │      │
│  │  - POST /api/db/{engine}/{instanceId}/backup      │      │
│  │  - POST /api/db/{engine}/{instanceId}/restore     │      │
│  │  - POST /api/k8s/workloads/{ns}/{name}:restart    │      │
│  │  - POST /api/sandboxes                            │      │
│  │  - GET  /api/tasks, /api/projects, /api/health    │      │
│  └──────────────────────────────────────────────────┘      │
│                           │                                  │
│                           │                                  │
│  ┌──────────────────────────────────────────────────┐      │
│  │         SignalR Hub (/hubs/tasks)                 │      │
│  │  - Real-time task updates                         │      │
│  │  - Broadcast progress to all clients              │      │
│  └──────────────────────────────────────────────────┘      │
│                           │                                  │
│                           │                                  │
│  ┌──────────────────────────────────────────────────┐      │
│  │    InMemoryMessageBus (Singleton)                 │      │
│  │  - Session-based FIFO queues                      │      │
│  │  - Key: sessionId = entityId                      │      │
│  │  - Ensures sequential processing per resource     │      │
│  └──────────────────────────────────────────────────┘      │
│                           │                                  │
│                           │                                  │
│  ┌──────────────────────────────────────────────────┐      │
│  │    TaskWorker (IHostedService)                    │      │
│  │  - Pulls messages from bus                        │      │
│  │  - Executes orchestration handlers                │      │
│  │  - Updates task status in DB                      │      │
│  │  - Sends SignalR updates                          │      │
│  └──────────────────────────────────────────────────┘      │
│                           │                                  │
│                           │                                  │
│  ┌──────────────────────────────────────────────────┐      │
│  │         EF Core DbContext (SQLite/PostgreSQL)     │      │
│  │  - Tasks, Projects, Environments                  │      │
│  │  - JSON conversion for complex types              │      │
│  └──────────────────────────────────────────────────┘      │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Task Processing Flow

### 1. Task Creation
```
Client → API POST /api/deployments
  ↓
API creates TaskEntity (status: "queued")
  ↓
API publishes TaskMessage to InMemoryMessageBus
  - sessionId = entityId (e.g., envId)
  - Ensures FIFO per resource
  ↓
API returns { taskId } to client
```

### 2. Task Execution
```
TaskWorker (background service)
  ↓
Receives message from InMemoryMessageBus
  - Locks session (one task at a time per entityId)
  ↓
Updates task status to "running"
  ↓
Sends SignalR update to all clients
  ↓
Executes orchestration handler:
  - DeploymentHandler
  - DbBackupHandler
  - DbRestoreHandler
  - RestartPodsHandler
  - SandboxHandler
  ↓
Each step:
  - Updates task.Steps[] in database
  - Sends SignalR progress updates
  - Writes artifacts to local storage
  ↓
Updates task status to "completed" or "failed"
  ↓
Releases session lock
  ↓
Completes message
```

### 3. Real-Time Updates
```
TaskWorker sends update
  ↓
SignalR Hub broadcasts to all connected clients
  ↓
Next.js frontend receives update
  ↓
UI updates task status, progress bar, logs
```

## Key Components

### InMemoryMessageBus
- **Purpose**: Queue orchestration with session-based processing
- **Implementation**: ConcurrentDictionary of Channels
- **Session ID**: entityId (ensures sequential processing per resource)
- **Concurrency**: One active task per session, multiple sessions in parallel
- **Demo Mode**: In-memory implementation
- **Production**: Can be swapped with Azure Service Bus

### TaskWorker (IHostedService)
- **Lifecycle**: Runs in background within API process
- **Message Loop**: Continuously polls for messages
- **Error Handling**: Retries with exponential backoff
- **Logging**: Serilog structured logging
- **Real-Time**: Direct access to SignalR HubContext

### Orchestration Handlers

#### DeploymentHandler
1. Validate parameters
2. Generate deployment plan → save to `data/artifacts/`
3. Apply changes (simulated)
4. Mark complete

#### DbBackupHandler
1. Prepare backup
2. Stream to storage → save to `data/backups/`
3. Mark complete

#### DbRestoreHandler
1. Validate artifact
2. Restore database (simulated)
3. Mark complete

#### RestartPodsHandler
1. Connect to Kubernetes (simulated)
2. Restart pods
3. Mark complete

#### SandboxHandler
1. Create sandbox environment
2. Set TTL for auto-expiry
3. Mark complete

## Database Schema

### TaskEntity
- `Id` (Guid, PK)
- `Type` (string) - deployment, db_backup, db_restore, etc.
- `Status` (string) - queued, running, completed, failed
- `EntityId` (string) - Resource identifier (used as sessionId)
- `CorrelationId` (string, nullable)
- `IdempotencyKey` (string, nullable, unique)
- `CreatedAt`, `UpdatedAt` (DateTime)
- `Steps` (List<TaskStep>, JSON) - Progress steps
- `Metadata` (Dictionary<string, string>, JSON)

### Project
- `Id` (Guid, PK)
- `Name`, `Description` (string)
- `CreatedAt` (DateTime)
- `TeamsWebhookUrl` (string, nullable)
- `EmailRecipients` (List<string>, JSON)
- `Environments` (navigation)

### Environment
- `Id` (Guid, PK)
- `ProjectId` (Guid, FK)
- `Name` (string)
- `Type` (string) - sandbox, dev, stage, prod
- `TtlMinutes` (int, nullable) - For sandboxes
- `ExpiresAt` (DateTime, nullable)
- `CreatedAt` (DateTime)

## Demo Mode vs Production

| Feature | Demo Mode | Production |
|---------|-----------|------------|
| Database | SQLite | PostgreSQL |
| Message Bus | In-Memory (same process) | Azure Service Bus (sessions) |
| Storage | Local filesystem | Azure Blob Storage |
| Kubernetes | Simulated | Real cluster via official client |
| Email | Console output | SendGrid |
| Teams | Real webhook (optional) | Real webhook |
| Auth | Mock user | Azure Entra ID (OIDC) |

## Extension Points

### IMessageBus
```csharp
public interface IMessageBus
{
    Task PublishAsync(TaskMessage message, CancellationToken ct = default);
    Task<TaskMessage?> ReceiveAsync(string? sessionId = null, CancellationToken ct = default);
    Task CompleteAsync(TaskMessage message, CancellationToken ct = default);
}
```

**Implementations:**
- `InMemoryMessageBus` (demo)
- `AzureServiceBusMessageBus` (production, commented/stubbed)

### IArtifactStore (Future)
```csharp
public interface IArtifactStore
{
    Task SaveAsync(string path, Stream content, CancellationToken ct = default);
    Task<Stream> GetAsync(string path, CancellationToken ct = default);
}
```

### IKubernetesClient (Future)
```csharp
public interface IKubernetesClient
{
    Task RestartWorkloadAsync(string ns, string name, CancellationToken ct = default);
}
```

## Security Considerations

### Demo Mode
- Mock user authentication (no real security)
- All endpoints accessible
- Suitable for development only

### Production Mode
- Azure Entra ID (OIDC) authentication
- Role-based authorization (viewer, operator, admin)
- API key rotation via Azure Key Vault
- HTTPS only
- Input validation via FluentValidation

## Monitoring & Observability

### Logging
- Serilog structured logging
- Console output in demo
- Application Insights in production

### Metrics (Planned)
- Task execution duration
- Queue depth per session
- Success/failure rates
- Resource utilization

### Tracing (Planned)
- OpenTelemetry integration
- Distributed tracing across components
- Export to Azure Monitor

## Deployment

### Local Development (Replit)
```bash
bash scripts/dev.sh
```
- Starts API + Worker on port 5056
- Starts Next.js on port 5000
- Uses SQLite database
- In-memory message bus

### Production (Azure)
1. Deploy API to Azure App Service
2. Configure Azure Service Bus with sessions
3. Set up PostgreSQL (Azure Database for PostgreSQL)
4. Configure Azure Key Vault for secrets
5. Enable Application Insights
6. Set environment variables

## Future Enhancements

1. **Idempotency**: Full support for `Idempotency-Key` header
2. **DLQ**: Dead letter queue for failed tasks
3. **Retry Logic**: Configurable retry policies
4. **SSE Fallback**: Server-Sent Events for environments without WebSockets
5. **RBAC**: Fine-grained role-based access control
6. **Audit Log**: Complete audit trail for all operations
7. **Cost Tracking**: Integration with Azure Retail Prices API
8. **Multi-Tenancy**: Support for multiple organizations
