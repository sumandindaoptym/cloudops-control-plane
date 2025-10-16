# CloudOps Control Plane

## Overview
This project is an Enterprise CloudOps Control Plane, serving as Optym's first developer platform monorepo. Its primary purpose is to streamline cloud operations through features like queue-based task orchestration, real-time updates via SignalR, one-click deployments, database management (backup/restore), pod restarts, and automated notifications (Teams/email). The platform aims to ease daily DevOps tasks and provide a unified control plane for managing cloud resources.

## User Preferences
- Focus on enterprise-grade architecture
- Prefer C# best practices and async/await patterns
- Value reliability and correctness over speed
- Comprehensive error handling and logging

## System Architecture
The platform is built as a monorepo with the following core technologies and design patterns:

**Frontend:**
- **Technology**: Next.js 15 (App Router, TypeScript, Tailwind CSS).
- **UI/UX**: Adheres to Optym's "Olympus" brand guidelines, featuring a clean, minimal dark theme.
    - **Color System**: Uses CSS custom properties for a defined palette (e.g., `hsl(215, 20%, 14%)` for background, `hsl(204, 100%, 59%)` for primary cyan accents) and translucent variants for badges. All components use CSS variables.
    - **Landing Page**: Features Optym logo, "Sign in with Microsoft" CTA, and feature cards highlighting one-click deploy, database management, real-time analytics, and secure access.
    - **Dashboard**: Layout includes an Azure subscription selector, sidebar navigation, stat cards, recent activity feed, quick action buttons, system status, and project/environment grids.
    - **Authentication UI**: Integrates Azure AD authentication via NextAuth.js v5, displaying user info and handling sign-out.

**Backend:**
- **Technology**: ASP.NET Core 9.0 with Minimal APIs.
- **Monorepo Structure**: `services/api` (API with integrated Worker), `services/shared` (shared libraries/models), `web` (Next.js frontend).
- **Message Bus & Task Processing**:
    - Uses `InMemoryMessageBus` (for demo) with `Azure Service Bus` planned for production.
    - Features session-based FIFO task processing.
    - `TaskWorker` runs as an `IHostedService` within the API process, ensuring single-process message delivery and shared singleton access.
    - Implemented a critical fix for semaphore handling to guarantee sequential processing per entityId and prevent deadlocks.
- **API Endpoints**: Provides endpoints for deployments, database operations (backup/restore), pod restarts, sandbox creation, and task/project retrieval.
- **Orchestration Handlers**: Dedicated handlers for various operations (e.g., DeploymentHandler, DbBackupHandler, RestartPodsHandler), which write artifacts to a designated directory.
- **Real-time Updates**: `SignalR` is integrated via `TaskHub` at `/hubs/tasks` for real-time task status, progress, and log updates.

**Database Layer:**
- **Technology**: EF Core with SQLite (for demo) and PostgreSQL (for production).
- **Models**: `TaskEntity`, `Project`, `Environment` with JSON converters for complex types.
- **Migration**: Database migrations are in place.

**Development Environment:**
- A `dev.sh` script starts both API (port 5056) and Frontend (port 5000).
- Swagger UI is available for API documentation.

## Recent Changes (October 11, 2025)
- **Added loading indicator**: Cyan progress bar appears at top during page navigation (nextjs-toploader)
- **Created dashboard navigation pages**: Added 6 new pages accessible via sidebar
  - Deployments (/dashboard/deployments) - View deployment history, stats, create new deployments
  - Databases (/dashboard/databases) - Manage database instances, backups, restores (needs backend integration)
  - Tasks (/dashboard/tasks) - Monitor background tasks with filtering
  - Projects (/dashboard/projects) - List and manage cloud projects
  - Environments (/dashboard/environments) - Manage deployment environments
  - Settings (/dashboard/settings) - Platform configuration (needs backend persistence)
- **Updated dashboard layout**: Sidebar now appears on all dashboard pages
- **Updated navigation header**: Shows Azure subscription selector and Sign Out button with user info
- **Known limitations**: Pages need improved error handling; some need full backend integration

## External Dependencies
- **Authentication**: Azure Active Directory (Azure AD) via NextAuth.js.
- **Database**: SQLite (local demo), PostgreSQL (production).
- **Message Queue**: InMemoryMessageBus (local demo), Azure Service Bus (production).
- **Real-time Communication**: SignalR.
- **Cloud Providers**: Azure (for subscription management and resource interaction).
- **Notifications**: Teams, Email (planned integrations).
- **Asset Hosting**: `web/public/optym-logo.png` for Optym logo.