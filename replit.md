# CloudOps Control Plane - Project Memory

## Project Overview
Enterprise CloudOps Control Plane - Optym's first developer platform monorepo with queue-based task orchestration, real-time updates via SignalR, one-click deployments, database backup/restore, pod restarts, and Teams/email notifications.

## Architecture
- **Backend**: ASP.NET Core 9.0 with Minimal APIs
- **Frontend**: Next.js 15 (App Router, TypeScript, Tailwind)
- **Database**: SQLite (demo), PostgreSQL (production)
- **Message Bus**: InMemoryMessageBus (demo), Azure Service Bus (production)
- **Real-time**: SignalR for live task updates
- **Orchestration**: Session-based FIFO task processing

## Current Status (October 11, 2025)

### ✅ Completed Features

0. **Authentication & Authorization**
   - Azure AD authentication via NextAuth.js v5 (beta)
   - Landing page with "Sign in with Microsoft" button
   - Protected dashboard accessible only after login
   - Session management with JWT strategy
   - User info display and sign-out functionality
   - Environment secrets: AZURE_AD_CLIENT_ID, AZURE_AD_CLIENT_SECRET, AZURE_AD_TENANT_ID, NEXTAUTH_SECRET, NEXTAUTH_URL

0.5. **Olympus Theme UI**
   - Clean, minimal design matching Olympus brand guidelines with exact HSL color specifications
   - **Color System** (CSS Custom Properties):
     - Background: hsl(215, 20%, 14%) - Main page background
     - Card: hsl(215, 30%, 8%) - Card backgrounds
     - Border: hsl(215, 20%, 24%) - All borders
     - Primary: hsl(204, 100%, 59%) - Cyan accent color
     - Secondary: hsl(215, 20%, 20%) - Secondary elements
     - Success: hsl(122, 39%, 55%) - Success indicators
     - Destructive: hsl(0, 62.8%, 50%) - Error/destructive actions
     - **Translucent Variants** (for badges):
       - --primary-bg: hsl(204 100% 59% / 0.2)
       - --secondary-bg: hsl(215 20% 20% / 0.5)
       - --success-bg: hsl(122 39% 55% / 0.2)
       - --destructive-bg: hsl(0 62.8% 50% / 0.2)
   - **Implementation**:
     - All colors defined as CSS custom properties in web/app/globals.css
     - All components use var(--color-name) - zero hardcoded colors
     - Variables exposed via @theme inline for Tailwind integration
   - **Landing Page**:
     - Navigation header with Optym logo image and "CloudOps" branding (no sign-in button)
     - Hero section with large heading and "Sign in With Microsoft" button with Microsoft logo
     - 4 feature cards with cyan icons (One-Click Deploy, Database Management, Real-time Analytics, Secure Access)
     - CTA section with simplified message about easing daily DevOps tasks
   - **Dashboard Components**:
     - StatCard: Stat cards with clean borders and hover effects
     - TaskItem: Activity feed items with status-based translucent badges
     - Sidebar: Navigation sidebar with active state highlighting and translucent badges
   - **Visual Effects**:
     - Solid dark backgrounds using exact HSL values
     - Cards with precise background and border colors
     - Primary buttons with exact cyan specification
     - Clean hover states with cyan accents
     - Simple, professional aesthetic
   - **Dashboard Layout**:
     - Sidebar navigation with icons and translucent badges
     - Stats overview with trend indicators
     - Recent activity feed with color-coded status indicators
     - Quick action buttons (Deploy, Backup, Restart, Sandbox)
     - System status monitoring
     - Projects grid with environment counts and translucent badges
   - Files: web/app/globals.css, web/app/page.tsx, web/app/dashboard/components/{StatCard,TaskItem,Sidebar}.tsx
1. **Monorepo Structure**
   - services/api: ASP.NET Core API with integrated Worker
   - services/shared: Shared libraries, models, DTOs
   - services/worker: (legacy, Worker now runs as IHostedService in API)
   - web: Next.js frontend
   - docs: Architecture documentation

2. **Database Layer**
   - EF Core with SQLite for demo mode
   - Models: TaskEntity, Project, Environment
   - JSON converters for complex types (Steps, Metadata, EmailRecipients)
   - Database migrations working
   - Seed data removed due to converter compatibility

3. **Message Bus & Task Processing**
   - InMemoryMessageBus with session-based FIFO queues
   - **CRITICAL FIX**: Semaphore handling fully corrected
     - ReceiveAsync acquires lock, holds if message received
     - CompleteAsync releases lock after processing
     - Finally blocks ensure cleanup on all exit paths
     - No more SemaphoreFullException or deadlocks
   - TaskWorker runs as IHostedService in API process
   - Guaranteed sequential processing per entityId

4. **API Endpoints**
   - POST /api/deployments - Create deployment tasks
   - POST /api/db/{engine}/{instanceId}/backup - Database backups
   - POST /api/db/{engine}/{instanceId}/restore - Database restores
   - POST /api/k8s/workloads/{namespace}/{name}:restart - Pod restarts
   - POST /api/sandboxes - Sandbox environment creation
   - GET /api/tasks - List all tasks
   - GET /api/tasks/{id} - Get task by ID
   - GET /api/projects - List projects
   - GET /api/health - Health check

5. **Orchestration Handlers**
   - DeploymentHandler: Validate → Plan → Apply → Complete
   - DbBackupHandler: Prepare → Stream → Complete
   - DbRestoreHandler: Validate → Restore → Complete
   - RestartPodsHandler: Connect → Restart → Complete
   - SandboxHandler: Create → Set TTL → Complete
   - All handlers write artifacts to data/artifacts/
   - Artifacts directory: services/api/CloudOps.Api/data/artifacts/

6. **SignalR Integration**
   - TaskHub at /hubs/tasks
   - Real-time updates: task status, progress, logs
   - Connected to TaskWorker for live broadcasts

7. **Development Environment**
   - Workflow "CloudOps Dev Server" runs both API and Next.js
   - API on port 5056 (integrated Worker)
   - Frontend on port 5000
   - Single dev.sh script for startup
   - Swagger UI at http://localhost:5056/swagger

### 🔧 Technical Decisions

#### Message Bus Architecture (CRITICAL)
**Problem**: Original design had separate Worker process, couldn't share InMemoryMessageBus
**Solution**: Integrated Worker into API as IHostedService (TaskWorker)
**Benefit**: Single process, shared singleton, guaranteed message delivery

#### Semaphore Lifecycle (CRITICAL FIX)
**Problem**: Multiple bugs causing double-release and deadlocks
**Final Solution**:
```csharp
// ReceiveAsync: Acquire lock, return message, hold lock if message exists
var lockAcquired = false;
TaskMessage? message = null;
try {
    await lockObj.WaitAsync(cancellationToken);
    lockAcquired = true;
    if (channel.Reader.TryRead(out message)) {
        return message; // Lock stays held
    }
    return null;
} finally {
    if (lockAcquired && message == null) {
        lockObj.Release(); // Release only if no message
    }
}

// CompleteAsync: Release lock after processing
lockObj.Release();

// TaskWorker: Ensure completion on all paths
try {
    await ProcessTaskAsync(message, stoppingToken);
} finally {
    await _messageBus.CompleteAsync(message, stoppingToken);
}
```

#### Database Seed Data
**Issue**: EF Core seed data incompatible with JSON converters
**Solution**: Removed seed data, projects/environments added via API

### 📁 Important Files
- `services/api/CloudOps.Api/Program.cs` - API startup, DI configuration
- `services/api/CloudOps.Api/TaskWorker.cs` - Background worker (IHostedService)
- `services/shared/CloudOps.Shared/Services/InMemoryMessageBus.cs` - Message bus with fixed semaphore
- `services/shared/CloudOps.Shared/Data/CloudOpsDbContext.cs` - EF Core context
- `services/shared/CloudOps.Shared/Models/TaskEntity.cs` - Task domain model
- `web/app/page.tsx` - Next.js home page
- `scripts/dev.sh` - Development startup script
- `docs/architecture.md` - Comprehensive architecture documentation
- `README.md` - Project readme
- `.gitignore` - Excludes data directories and build artifacts

### 🚀 Running the Project
```bash
bash scripts/dev.sh
```
- API + Worker: http://localhost:5056 (Swagger: http://localhost:5056/swagger)
- Frontend: http://localhost:5000
- Database: data/platform/platform.db (SQLite)
- Artifacts: services/api/CloudOps.Api/data/artifacts/
- Backups: services/api/CloudOps.Api/data/backups/

### 🧪 Testing
**Verified Working:**
- Task creation and processing ✅
- Sequential FIFO processing per session ✅
- No semaphore exceptions ✅
- No deadlocks ✅
- Artifact creation ✅
- Database persistence ✅

**Test Command:**
```bash
curl -X POST http://localhost:5056/api/deployments \
  -H "Content-Type: application/json" \
  -d '{"environmentId":"test","version":"v1.0.0","strategy":"rolling"}'
```

### 🔮 Next Steps (Future Enhancements)
1. **Frontend Development**
   - SignalR client integration for real-time task updates
   - Additional dashboard pages (Deployments, Databases, Tasks, Projects, Environments, Settings)
   - Project/environment management UI with CRUD operations
   - Task detail views and logs streaming

2. **Production Readiness**
   - Azure Service Bus implementation
   - PostgreSQL migration
   - Azure Blob Storage for artifacts
   - Kubernetes integration (real client)
   - Authentication (Azure Entra ID)
   - Email notifications (SendGrid)

3. **Testing & Quality**
   - Unit tests for message bus
   - Integration tests for task processing
   - Stress testing for concurrency
   - Regression tests for semaphore lifecycle

4. **Features**
   - Idempotency support (via Idempotency-Key header)
   - Dead letter queue for failed tasks
   - Configurable retry policies
   - Cost tracking integration

## User Preferences
- Focus on enterprise-grade architecture
- Prefer C# best practices and async/await patterns
- Value reliability and correctness over speed
- Comprehensive error handling and logging

## Development History
- **October 11, 2025** (Latest):
  - Updated landing page UX: removed top-right sign-in button, simplified navigation
  - Changed hero CTA to "Sign in With Microsoft" with Microsoft logo icon
  - Replaced "Task Tracking" feature card with "Database Management" card
  - Simplified CTA section message to focus on easing DevOps tasks
  
- **October 11, 2025** (Earlier):
  - Integrated Optym brand logo (two-color: blue arc + white text) in navigation
  - Logo displays on both landing page and dashboard header
  - Logo stored in web/public/optym-logo.png for optimal Next.js asset handling
  
- **October 11, 2025** (Earlier):
  - Implemented comprehensive CSS custom property system with exact HSL color codes
  - Created translucent badge variants using modern CSS syntax: hsl(204 100% 59% / 0.2)
  - Updated all components to use CSS variables exclusively (zero hardcoded colors)
  - Fixed Sidebar and TaskItem badges to use translucent backgrounds
  - All status indicators (running, completed, failed, pending) use correct translucent colors
  - Exposed all color variables via @theme inline for Tailwind integration
  - Complete theme implementation architect-verified and production-ready

- **October 11, 2025** (Earlier):
  - Updated entire theme to match exact Olympus design specifications
  - Implemented clean, minimal UI with solid dark backgrounds
  - Changed from gradient-based design to solid cyan accents
  - Updated landing page with Olympus branding, feature cards, and CTA section
  - Simplified dashboard components removing glassmorphism effects
  - Created cleaner card designs with simple borders and hover states
  - Updated navigation header to match Olympus brand (optym + CloudOps)
  - All pages now use consistent Olympus color scheme and styling

- **October 11, 2025** (Earlier):
  - Implemented glassmorphism/liquid-glass dashboard design (replaced with Olympus theme)
  - Created reusable components: StatCard, TaskItem, Sidebar
  - Built dashboard with sidebar navigation, stats overview, activity feed, quick actions, and system status

- **October 11, 2025** (Earlier):
  - Implemented Next.js API proxy for frontend-backend communication
  - Fixed API connectivity issues (no CORS, works in all environments)
  - Created API connectivity documentation
  - Frontend now successfully connects to backend via `/api` proxy
  - System fully operational with working UI and API integration

- **October 10, 2025**: 
  - Fixed critical semaphore bugs in InMemoryMessageBus
  - Integrated Worker into API process as IHostedService
  - Removed database seed data due to EF Core converter issues
  - Created architecture documentation
  - System now fully operational with all critical bugs resolved
