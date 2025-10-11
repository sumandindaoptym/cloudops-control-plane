# API Connectivity Guide

## How the Frontend Accesses the Backend API

### Architecture Overview

```
┌─────────────────────────────────────────────┐
│         User's Browser                      │
│                                             │
│  Next.js App (http://yourdomain.com)       │
│                                             │
│  Calls: /api/health, /api/projects, etc.   │
└───────────────────┬─────────────────────────┘
                    │
                    │ HTTP Request
                    ▼
┌─────────────────────────────────────────────┐
│     Next.js API Proxy (Port 5000)           │
│                                             │
│  Route: /app/api/[...proxy]/route.ts       │
│  Matches: /api/* requests                   │
└───────────────────┬─────────────────────────┘
                    │
                    │ Proxies to localhost
                    ▼
┌─────────────────────────────────────────────┐
│   ASP.NET Core API (Port 5056)             │
│                                             │
│   Actual backend endpoints                  │
│   - POST /api/deployments                   │
│   - GET  /api/health                        │
│   - GET  /api/projects                      │
│   - etc.                                    │
└─────────────────────────────────────────────┘
```

### Implementation Details

#### 1. Frontend API Client (`web/lib/api.ts`)

The frontend uses a centralized API client:

```typescript
// Returns '/api' for browser, 'http://localhost:5056' for server
export function getApiUrl(): string {
  if (typeof window === 'undefined') {
    return 'http://localhost:5056'; // Server-side
  }
  return '/api'; // Client-side (uses proxy)
}

// Wrapper for API calls
export async function apiFetch(endpoint: string, options?: RequestInit) {
  const url = `${API_URL}${endpoint}`;
  const response = await fetch(url, options);
  return response.json();
}
```

**Usage in components:**
```typescript
// Call like this (no /api prefix needed):
apiFetch('/health')      // → /api/health
apiFetch('/projects')    // → /api/projects
apiFetch('/deployments') // → /api/deployments
```

#### 2. Next.js API Proxy (`web/app/api/[...proxy]/route.ts`)

Next.js 15 API route that proxies all `/api/*` requests to the backend:

```typescript
// Matches: /api/health, /api/projects, etc.
// Proxies to: http://localhost:5056/api/health, etc.

export async function GET(request, { params }) {
  const { proxy } = await params; // ['health'], ['projects'], etc.
  
  // Build backend URL
  const url = `http://localhost:5056/api/${proxy.join('/')}`;
  
  // Forward the request
  const response = await fetch(url, { 
    method: 'GET',
    headers: request.headers 
  });
  
  return NextResponse.json(await response.json());
}
```

**Benefits:**
- ✅ **No CORS issues** - Same origin for browser
- ✅ **Works everywhere** - Replit, local, production
- ✅ **Transparent** - Frontend doesn't know about backend URL
- ✅ **Secure** - Backend URL not exposed to browser

#### 3. Backend Configuration

The ASP.NET Core API listens on `0.0.0.0:5056`:

```csharp
// Program.cs
app.Run("http://0.0.0.0:5056");
```

This allows the Next.js server (running on the same machine) to access it via `http://localhost:5056`.

### Port Configuration in Replit

From `.replit` file:

```toml
[[ports]]
localPort = 5000    # Next.js frontend
externalPort = 80   # Public access (no port needed in URL)

[[ports]]
localPort = 5056    # ASP.NET Core API
externalPort = 3000 # (Not used - proxy handles it)
```

**Why we use a proxy instead of external port 3000:**
- External ports can be firewalled or restricted
- CORS configuration is complex
- Proxy is more reliable and easier to configure

### Request Flow Example

**User clicks "One-Click Deploy":**

1. **Frontend** calls:
   ```javascript
   apiFetch('/deployments', {
     method: 'POST',
     body: JSON.stringify({ envId: '...', version: '...' })
   })
   ```

2. **Request goes to:** `GET https://yourdomain.com/api/deployments`

3. **Next.js proxy** receives it and forwards to:
   ```
   POST http://localhost:5056/api/deployments
   ```

4. **ASP.NET Core API** processes the request:
   ```csharp
   app.MapPost("/api/deployments", async (DeploymentRequest req, ...) => {
     // Create task, publish to message bus
     return Results.Ok(new { taskId });
   });
   ```

5. **Response flows back** through proxy to browser

### Local Development

When running locally (not in Replit):

```bash
# Terminal 1: Start backend
cd services/api/CloudOps.Api
dotnet run

# Terminal 2: Start frontend
cd web
npm run dev

# Both services accessible at:
# - Frontend: http://localhost:5000
# - API (direct): http://localhost:5056
# - API (via proxy): http://localhost:5000/api/*
```

### Production Deployment

For production, update `BACKEND_URL` environment variable:

```env
# .env.production
BACKEND_URL=http://internal-api-service:5056
```

Or use relative URLs if API is on same server:
```env
BACKEND_URL=http://localhost:5056
```

### Troubleshooting

**Frontend can't reach API:**
1. Check both services are running: `bash scripts/dev.sh`
2. Verify proxy route exists: `web/app/api/[...proxy]/route.ts`
3. Check console for errors
4. Test proxy directly: `curl http://localhost:5000/api/health`

**CORS errors (shouldn't happen with proxy):**
- Proxy eliminates CORS because requests are same-origin
- If you see CORS errors, the proxy isn't working

**404 errors on API routes:**
- Make sure frontend calls don't include `/api` prefix:
  - ✅ Correct: `apiFetch('/health')`
  - ❌ Wrong: `apiFetch('/api/health')`

### Key Files

- **Frontend API client**: `web/lib/api.ts`
- **API Proxy**: `web/app/api/[...proxy]/route.ts`
- **Backend startup**: `services/api/CloudOps.Api/Program.cs`
- **Dev script**: `scripts/dev.sh`
- **Replit config**: `.replit`

### Testing API Connectivity

```bash
# Test backend directly
curl http://localhost:5056/api/health

# Test through Next.js proxy
curl http://localhost:5000/api/health

# Both should return:
# {"status":"healthy","timestamp":"..."}
```

## Summary

The frontend accesses the backend through a **Next.js API proxy**:

1. Browser calls `/api/*` (same domain, no CORS)
2. Next.js proxy forwards to `http://localhost:5056/api/*`
3. Backend processes and responds
4. Response flows back through proxy to browser

This architecture provides a reliable, secure, and maintainable way to connect frontend and backend services across all environments! 🎉
