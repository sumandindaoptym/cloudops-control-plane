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
- **Technology**: ASP.NET Core 9.0 Razor Pages with custom CSS (olympus.css).
- **UI/UX**: Adheres to Optym's "Olympus" brand guidelines, featuring a clean, minimal dark theme.
    - **Color System**: Uses CSS custom properties for a modern dark palette with deep backgrounds and teal accents. All components use CSS variables.
      - Background: `hsl(220, 15%, 9%)` - Deep dark background
      - Card: `hsl(220, 15%, 6%)` - Even darker card backgrounds
      - Border: `hsl(220, 15%, 18%)` - Subtle borders
      - Primary: `hsl(175, 70%, 50%)` - Teal accent color
      - Translucent variants for badges with 15% opacity
    - **Landing Page**: Features Optym logo, "Sign in with Microsoft" CTA, and feature cards highlighting one-click deploy, database management, real-time analytics, and secure access.
    - **Dashboard**: Layout includes an Azure subscription selector, sidebar navigation, stat cards, recent activity feed, quick action buttons, system status, and project/environment grids.
    - **Pages**: Landing page, Dashboard (main), Deployments, Databases, Tasks, Service Bus, Projects, Environments, and Settings.
    - **Authentication**: Integrates Azure AD authentication via Microsoft.Identity.Web and Microsoft.Identity.Web.UI packages, providing native ASP.NET Core authentication with automatic token management.

**Backend:**
- **Technology**: ASP.NET Core 9.0 with Minimal APIs.
- **Monorepo Structure**: `services/api` (API with integrated Worker), `services/shared` (shared libraries/models), `services/web` (ASP.NET Razor Pages frontend).
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

## Recent Changes (November 20, 2025)
- **Migrated frontend from Next.js to ASP.NET Razor Pages**:
  - Replaced Next.js 15 with ASP.NET Core 9.0 Razor Pages
  - Implemented native Azure AD authentication using Microsoft.Identity.Web middleware
  - Created all 7 pages: Landing (Index), Dashboard, Deployments, Databases, Tasks, Projects, Environments, and Settings
  - Maintained complete Olympus dark theme design with custom CSS (olympus.css)
  - Updated dev.sh script to run ASP.NET web app on port 5000
  - Removed Next.js project directory and dependencies
  - **Fixed HTTPS redirect issue**: Added ForwardedHeaders middleware and OpenIdConnect event handler
    - Configured ForwardedHeaders to trust X-Forwarded-Proto and X-Forwarded-Host headers from Replit's proxy
    - Added OnRedirectToIdentityProvider event to explicitly replace http:// with https:// in OAuth redirect URIs
    - OAuth redirects now correctly use HTTPS instead of HTTP
    - Resolves "Unsafe attempt to initiate navigation" and blank page issues during sign-in
  - **Fixed Microsoft Identity endpoints**: Added controller support for Microsoft.Identity.Web.UI
    - Added `AddControllersWithViews()` to services configuration
    - Added `MapControllers()` to enable MVC controller routing
    - Microsoft Identity sign-in/sign-out endpoints now properly accessible at `/MicrosoftIdentity/Account/*`
  - **Fixed post-login redirect**: Configured authentication to redirect to dashboard after successful sign-in
    - Added `OnTicketReceived` event handler to set redirect URI to `/Dashboard`
    - Modified landing page to automatically redirect authenticated users to dashboard
    - Users now land on the dashboard after signing in with Microsoft
  - **Authentication Configuration**: Azure AD App Registration must have redirect URI configured:
    - Redirect URI format: `https://<your-replit-domain>/signin-oidc`
    - Example: `https://a46607d1-b410-4ba8-bd9c-95a49e37d57e-00-1clwqs1t2ts23.worf.replit.dev/signin-oidc`
    - Add this in Azure Portal → App Registrations → Authentication → Redirect URIs (Web platform)
    - After adding the redirect URI, login will redirect to Microsoft, then back to dashboard
  - Environment variables used: `AZURE_AD_TENANT_ID`, `AZURE_AD_CLIENT_ID`, `AZURE_AD_CLIENT_SECRET`

## Previous Changes (October 16, 2025)
- **Updated theme to modern dark design**: Complete color palette refresh
  - Much darker background (`hsl(220, 15%, 9%)`) for better contrast
  - Deeper card backgrounds (`hsl(220, 15%, 6%)`) 
  - Changed primary accent from cyan to teal (`hsl(175, 70%, 50%)`)
  - Reduced translucent badge opacity to 15% for subtlety
  - More refined border colors for cleaner separation
  - **Added gradient effects**: Subtle teal gradient overlay at top of pages for depth
    - Navigation header has vertical gradient for sophistication
    - Top gradient overlay fades from teal to transparent
    - Creates modern, polished Olympus design aesthetic
- **Enhanced UX interactions**:
  - Added cursor pointer styles for all interactive elements (buttons, links, selects)
  - Disabled elements show "not-allowed" cursor for better feedback
  - Updated sidebar to use usePathname for accurate active state tracking
- **Added loading indicator**: Teal progress bar appears at top during page navigation (nextjs-toploader)
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