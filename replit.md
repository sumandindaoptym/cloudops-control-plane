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

## Recent Changes (November 22, 2025 & 23)
- **Fixed sign-out redirect loop**:
  - Overrode default Microsoft Identity UI SignedOut page (`Areas/MicrosoftIdentity/Pages/Account/SignedOut.cshtml`) with custom animated design
  - Added `OnRedirectToIdentityProviderForSignOut` event handler to handle HTTPS redirect during sign-out
  - Added `[AllowAnonymous]` attribute to SignedOut page model
  - Configured RazorPages conventions to explicitly allow anonymous access: `options.Conventions.AllowAnonymousToPage("/MicrosoftIdentity/Account/SignedOut")`
  - Users now see funny animated sign-out page with waving emoji (👋), humorous messages, and 5-second countdown
  - Sign-out flow: Sign Out → Azure AD logout → /MicrosoftIdentity/Account/SignedOut (animated page with auto-redirect) → Landing page (unauthenticated)
- **Added animated button hover effects**:
  - Implemented exact animation from https://uiverse.io/vinodjangid07/heavy-badger-29
  - On hover, icon container expands while text slides and collapses (width: 70px/100px → 0, font-size → 0)
  - On click, button scales down for tactile feedback (0.95 scale)
  - Applied to both Refresh button (120px, icon 35px → 90px) and Purge-DLQ button (160px, icon 40px → 150px)
  - Uses Olympus teal gradient (`hsl(175, 70%, 55%)` to `hsl(175, 70%, 45%)`) for icon containers
  - Disabled buttons maintain opacity reduction without animations
  - Smooth 0.3s transitions matching original uiverse.io design pattern
- **Converted namespace dropdown to searchable filter**:
  - Replaced standard dropdown with search input field and custom dropdown list
  - Users can now type to search/filter Service Bus namespaces by name or location
  - Dropdown shows filtered results with namespace name and location
  - Supports click-to-select and automatic dropdown closure when clicking outside
  - Includes Font Awesome search icon for better UX
  - Maintains all existing auto-refresh functionality when subscription changes

## Previous Changes (November 21, 2025)
- **Enhanced Service Bus DLQ Cleaner UX**:
  - **Fixed namespace auto-refresh**: Namespace dropdown now automatically updates when Azure subscription changes in header
  - **Upgraded refresh button**: Replaced text button with Font Awesome refresh icon (`fa-sync-alt`)
  - **Created funny sign-out page**: Custom `/SignedOut` page with cartoons and auto-redirect
- **Fixed authentication scope issue (AADSTS28000)**:
  - Implemented incremental consent pattern: Azure Management scope during sign-in, Service Bus scope on-demand

## External Dependencies
- **Authentication**: Azure Active Directory (Azure AD).
- **Database**: SQLite, PostgreSQL.
- **Message Queue**: InMemoryMessageBus, Azure Service Bus.
- **Real-time Communication**: SignalR.
- **Cloud Providers**: Azure (for subscription management and resource interaction).
- **Notifications**: Teams, Email (planned).
- **Asset Hosting**: `web/public/optym-logo.png` for Optym logo.