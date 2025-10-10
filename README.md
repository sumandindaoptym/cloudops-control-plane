# CloudOps Control Plane

Enterprise-grade developer platform with queue-based task orchestration, real-time updates, and one-click operations.

## 🚀 Quick Start

### Prerequisites

- .NET SDK 9.0
- Node.js 22+
- pnpm

### Run in Development Mode

```bash
bash scripts/dev.sh
```

This starts all services:
- **API**: http://localhost:5056 (Swagger: http://localhost:5056/swagger)
- **Worker**: Background service
- **Web**: http://localhost:5000

### Services

#### API (Port 5056)
ASP.NET Core minimal API with:
- OpenAPI/Swagger documentation
- Entity Framework Core (SQLite in demo mode)
- SignalR for real-time updates
- Serilog structured logging

#### Worker Service
Background task processor with:
- In-memory session-based message bus (FIFO per resource)
- Task orchestration handlers
- SignalR client for real-time updates

#### Web Frontend (Port 5000)
Next.js application with:
- Modern dashboard UI
- Real-time task monitoring
- One-click action buttons
- Tailwind CSS styling

## 📋 Features

### Task Orchestration
- **Sequential Processing**: Queue ensures one task per resource/entity at a time
- **Session-Based**: Uses `sessionId = entityId` for FIFO ordering
- **Real-Time Updates**: Live progress via SignalR

### Operations

#### One-Click Deployment
Simulates 3-step deployment flow:
1. Validate parameters
2. Generate deployment plan
3. Apply changes

```bash
POST /api/deployments
{
  "projectId": "guid",
  "envId": "guid",
  "templateId": "string",
  "parameters": {}
}
```

#### Database Backup
```bash
POST /api/db/{engine}/{instanceId}/backup
```

#### Database Restore
```bash
POST /api/db/{engine}/{instanceId}/restore
{
  "artifactId": "string",
  "targetDatabase": "string"
}
```

#### Restart Pods
```bash
POST /api/k8s/workloads/{namespace}/{name}:restart
```

#### Create Sandbox
```bash
POST /api/sandboxes
{
  "projectId": "guid",
  "ttlMinutes": 60
}
```

### Cost Estimation
```bash
GET /api/cost/estimate
```

## 🔧 Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `DEMO_MODE` | `true` | Enable demo mode (SQLite, in-memory queue) |
| `PLATFORM_DB_CONN` | SQLite path | Database connection string |
| `AZURE_SERVICEBUS_CONNECTION_STRING` | - | Azure Service Bus (production) |
| `TEAMS_WEBHOOK_URL` | - | Microsoft Teams webhook |
| `SENDGRID_API_KEY` | - | SendGrid email (production) |
| `KUBECONFIG` | - | Kubernetes config (enables real K8s) |
| `MAX_CONCURRENT_SESSIONS` | `10` | Worker concurrency |

### Demo Mode vs Production

**Demo Mode** (default):
- SQLite database
- In-memory message bus
- Console email output
- Simulated operations

**Production Mode**:
- PostgreSQL database
- Azure Service Bus with sessions
- SendGrid email
- Real Azure/K8s operations

## 📁 Project Structure

```
/
├── services/
│   ├── api/              # ASP.NET Core API
│   ├── worker/           # Background worker service
│   └── shared/           # Shared models & DTOs
├── web/                  # Next.js frontend
├── data/                 # Local data storage (demo)
│   ├── artifacts/        # Deployment plans
│   ├── backups/          # Database backups
│   └── platform/         # SQLite database
├── scripts/
│   └── dev.sh            # Development startup script
└── docs/
    └── architecture.md   # Architecture documentation

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Frontend tests
cd web && pnpm test
```

## 🏗️ Architecture

The CloudOps Control Plane follows a clean architecture with:

1. **API Layer**: Receives requests, validates, creates tasks
2. **Message Bus**: Ensures sequential processing per resource
3. **Worker**: Executes orchestrated task flows
4. **Real-Time**: SignalR pushes updates to connected clients

See `docs/architecture.md` for detailed diagrams and flows.

## 🔐 Security

- **Demo Auth**: Mock user for development
- **Production**: Azure Entra ID (OIDC) ready
- **Secrets**: Environment variables + Azure Key Vault adapter
- **Validation**: FluentValidation on all inputs

## 📊 Monitoring

- **Logging**: Serilog to console (dev) / Application Insights (prod)
- **Tracing**: OpenTelemetry ready
- **Metrics**: Task duration, queue depth, success rates

## 🚢 Deployment

The project is optimized for Replit deployment:

1. All services start with single command
2. SQLite for zero-config database
3. In-memory queue for demo orchestration
4. Port 5000 for web (Replit-compatible)

### Production Deployment

For production on Azure:

1. Set `DEMO_MODE=false`
2. Configure PostgreSQL connection
3. Set up Azure Service Bus
4. Enable Azure Key Vault
5. Configure monitoring

## 📝 API Reference

Full API documentation available at: **http://localhost:5056/swagger**

Key endpoints:
- `GET /api/health` - Health check
- `GET /api/projects` - List projects
- `GET /api/tasks` - List tasks
- `GET /api/tasks/{id}` - Task details
- `POST /api/deployments` - Deploy
- `POST /api/db/{engine}/{instanceId}/backup` - Backup
- `POST /api/k8s/workloads/{ns}/{name}:restart` - Restart

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests
5. Submit a pull request

## 📄 License

MIT License - See LICENSE file for details
