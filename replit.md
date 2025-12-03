# CloudOps Control Plane

## Overview
This project is an Enterprise CloudOps Control Plane, serving as Optym's first developer platform monorepo. Its core purpose is to centralize and simplify cloud operations through a unified control plane for managing cloud resources. Key features include queue-based task orchestration, real-time status updates, one-click deployments, database management (backup/restore), pod restarts, and automated notifications. The platform aims to enhance operational efficiency and streamline daily DevOps tasks.

## User Preferences
- Focus on enterprise-grade architecture
- Prefer C# best practices and async/await patterns
- Value reliability and correctness over speed
- Comprehensive error handling and logging

## System Architecture
The platform is an ASP.NET Core 9.0 monorepo, built with the following architectural patterns and design decisions:

**Frontend:**
- **Technology**: ASP.NET Core 9.0 Razor Pages.
- **UI/UX**: Adheres to Optym's "Olympus" brand guidelines, featuring a clean, minimal dark theme using custom CSS with a modern dark palette and teal accents.
- **Authentication**: Integrates Azure AD authentication via `Microsoft.Identity.Web` and `Microsoft.Identity.Web.UI`, supporting incremental consent for API permissions.

**Backend:**
- **Technology**: ASP.NET Core 9.0 with Minimal APIs.
- **Monorepo Structure**: Organized into `services/api`, `services/shared`, and `services/web`.
- **Message Bus & Task Processing**: Uses `InMemoryMessageBus` (with `Azure Service Bus` planned for production) for session-based FIFO task processing. A `TaskWorker` operates as an `IHostedService` within the API for single-process message delivery and shared singleton access.
- **API Endpoints**: Provides endpoints for deployments, database operations, pod restarts, sandbox creation, and task/project retrieval.
- **Orchestration Handlers**: Dedicated handlers (e.g., `DeploymentHandler`, `DbBackupHandler`, `RestartPodsHandler`) manage specific operations.
- **Real-time Updates**: `SignalR` is integrated via `TaskHub` for real-time task status, progress, and log updates.

**Agent System:**
- **CloudOps Agent Application**: A .NET 9 console worker application for self-hosted agents, featuring a pull-based architecture with API key authentication, heartbeats, and job polling.
- **Agent Management**: Leverages an Azure DevOps-style Agent Pools model with `AgentPool` and `AgentLabel` entities for organizing and tagging agents.
- **Job Execution**: Agents execute jobs via handlers for database backup/restore, script execution, and pod restarts, with robust retry policies and logging.

**Database Layer:**
- **Technology**: EF Core with SQLite (development) and PostgreSQL (production).
- **Models**: `TaskEntity`, `Project`, `Environment`, `ActivityLog`, `AgentPool`, `AgentLabel`.
- **Migration**: Database migrations are implemented.

**Development Environment:**
- A `dev.sh` script facilitates local development, starting both API and Frontend. Swagger UI is available for API documentation.

## External Dependencies
- **Authentication**: Azure Active Directory (Azure AD).
- **Database**: SQLite, PostgreSQL.
- **Message Queue**: InMemoryMessageBus, Azure Service Bus.
- **Real-time Communication**: SignalR with Azure SignalR Service.
- **Cloud Providers**: Azure.
- **Notifications**: Teams, Email (planned).
- **Asset Hosting**: `web/public/optym-logo.png`.