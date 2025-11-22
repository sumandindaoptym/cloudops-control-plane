# CloudOps Control Plane

## Overview
This project is an Enterprise CloudOps Control Plane, designed as Optym's first developer platform monorepo. Its primary purpose is to streamline cloud operations by providing a unified control plane for managing cloud resources. Key capabilities include queue-based task orchestration, real-time updates, one-click deployments, database management (backup/restore), pod restarts, and automated notifications (Teams/email). The platform aims to simplify daily DevOps tasks and enhance operational efficiency.

## User Preferences
- Focus on enterprise-grade architecture
- Prefer C# best practices and async/await patterns
- Value reliability and correctness over speed
- Comprehensive error handling and logging

## System Architecture
The platform is built as a monorepo leveraging ASP.NET Core 9.0, following core architectural patterns and design decisions:

**Frontend:**
- **Technology**: ASP.NET Core 9.0 Razor Pages.
- **UI/UX**: Adheres to Optym's "Olympus" brand guidelines with a clean, minimal dark theme using custom CSS (olympus.css).
    - **Color System**: Utilizes CSS custom properties for a modern dark palette with deep backgrounds (`hsl(220, 15%, 9%)`), darker card backgrounds (`hsl(220, 15%, 6%)`), subtle borders (`hsl(220, 15%, 18%)`), and teal accents (`hsl(175, 70%, 50%)`).
    - **Pages**: Includes Landing, Dashboard, Deployments, Databases, Tasks, Service Bus, Projects, Environments, and Settings.
    - **Authentication**: Integrates Azure AD authentication via `Microsoft.Identity.Web` and `Microsoft.Identity.Web.UI` for native ASP.NET Core authentication and token management, supporting incremental consent for API permissions.

**Backend:**
- **Technology**: ASP.NET Core 9.0 with Minimal APIs.
- **Monorepo Structure**: Organized into `services/api` (API with integrated Worker), `services/shared` (shared libraries/models), and `services/web` (ASP.NET Razor Pages frontend).
- **Message Bus & Task Processing**: Uses `InMemoryMessageBus` (with `Azure Service Bus` planned for production) for session-based FIFO task processing. A `TaskWorker` runs as an `IHostedService` within the API for single-process message delivery and shared singleton access, incorporating semaphore handling for sequential processing.
- **API Endpoints**: Provides endpoints for deployments, database operations, pod restarts, sandbox creation, and task/project retrieval.
- **Orchestration Handlers**: Dedicated handlers (e.g., `DeploymentHandler`, `DbBackupHandler`, `RestartPodsHandler`) manage specific operations and write artifacts.
- **Real-time Updates**: `SignalR` is integrated via `TaskHub` at `/hubs/tasks` for real-time task status, progress, and log updates.

**Database Layer:**
- **Technology**: EF Core with SQLite (for demo) and PostgreSQL (for production).
- **Models**: `TaskEntity`, `Project`, `Environment` with JSON converters for complex types.
- **Migration**: Database migrations are implemented.

**Development Environment:**
- A `dev.sh` script facilitates local development by starting both API (port 5056) and Frontend (port 5000). Swagger UI is available for API documentation.

## External Dependencies
- **Authentication**: Azure Active Directory (Azure AD).
- **Database**: SQLite, PostgreSQL.
- **Message Queue**: InMemoryMessageBus, Azure Service Bus.
- **Real-time Communication**: SignalR.
- **Cloud Providers**: Azure (for subscription management and resource interaction).
- **Notifications**: Teams, Email (planned).
- **Asset Hosting**: `web/public/optym-logo.png` for Optym logo.