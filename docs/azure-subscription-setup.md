# Azure Subscription Integration

## Overview
The CloudOps platform integrates with Azure to fetch and display subscriptions that the **logged-in user** has access to. This allows users to select the appropriate subscription context for their operations based on their own Azure RBAC permissions.

## Current Implementation

### Backend
- **API Endpoint**: `GET /api/subscriptions` (Minimal API)
- **Service**: `AzureSubscriptionService` using HttpClient
- **Authentication**: User's Azure AD access token (delegated permissions via Microsoft.Identity.Web)
- **Token Acquisition**: `ITokenAcquisition` for obtaining user access tokens

### Frontend
- **Implementation**: Vanilla JavaScript in `_DashboardLayout.cshtml`
- **Features**:
  - Fetches subscriptions from API on page load
  - Displays in dropdown selector
  - Persists selection in localStorage
  - Shows loading state while fetching
  - Automatic error handling with helpful messages

## Azure AD Permissions Required

The implementation uses **user delegation** - the logged-in user's own permissions determine which subscriptions they can see.

### 1. **App Registration API Permissions**
The Azure AD app registration needs delegated permissions:

**Steps:**
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations**
3. Select your CloudOps app registration
4. Click **API permissions**
5. Click **+ Add a permission**
6. Select **Azure Service Management**
7. Check **user_impersonation** (Delegated permission)
8. Click **Add permissions**
9. Click **Grant admin consent** (admin consent required)

### 2. **User RBAC Permissions**
Each user needs appropriate Azure RBAC roles on subscriptions:

- Users will see **only** subscriptions where they have at least **Reader** role
- No additional configuration needed - uses user's existing Azure permissions
- Works with any Azure RBAC role (Reader, Contributor, Owner, etc.)

### 3. **ASP.NET Core Authentication Configuration**
The authentication is configured in `Program.cs` with the required scopes:

```csharp
options.Scope.Add("openid");
options.Scope.Add("profile");
options.Scope.Add("email");
options.Scope.Add("offline_access");
options.Scope.Add("https://management.azure.com/user_impersonation");
```

**Scopes Explained:**
- `openid profile email`: Basic user info
- `https://management.azure.com/user_impersonation`: Azure Resource Manager delegated access
- `offline_access`: Enables refresh tokens for automatic token renewal

### 4. **Token Acquisition**
The implementation uses Microsoft.Identity.Web for token management:
- `EnableTokenAcquisitionToCallDownstreamApi()` enables token acquisition
- `AddInMemoryTokenCaches()` caches tokens in memory
- Tokens are automatically refreshed before expiration
- No user interruption when tokens expire
- Graceful error handling with helpful messages

**Important:** Users must sign out and sign back in after the Azure Resource Manager scope is added to consent to the new permission.

## How It Works

### Authentication Flow
1. User signs in with Microsoft (Azure AD) via Microsoft.Identity.Web
2. Azure AD authenticates user and requests consent for scopes (including Azure Resource Manager)
3. Access token with Azure Resource Manager scope is stored in token cache
4. Frontend JavaScript calls `/api/subscriptions` endpoint
5. Backend uses `ITokenAcquisition` to get user's access token for Azure Management API
6. Backend calls Azure Resource Manager API with user's token
7. API returns subscriptions the user has access to based on their RBAC permissions

### Token Flow Diagram
```
User Browser → Microsoft.Identity.Web (Azure AD) → Access Token (with ARM scope)
                                                            ↓
Frontend (JavaScript fetch) → Backend API (/api/subscriptions)
                                                            ↓
                            ITokenAcquisition (gets user token from cache)
                                                            ↓
                            Azure Management API (https://management.azure.com)
                                                            ↓
                            Subscriptions (filtered by user's RBAC)
```

## Testing

### Test API Endpoint
You must be authenticated to test the API. Navigate to the dashboard after signing in, and the subscription dropdown will automatically fetch and display subscriptions.

Alternatively, use browser DevTools:
```javascript
// In browser console (while authenticated)
fetch('/api/subscriptions')
  .then(r => r.json())
  .then(data => console.log(data));
```

### Expected Response (with permissions)
```json
[
  {
    "subscriptionId": "xxx-xxx-xxx",
    "displayName": "Your Subscription Name",
    "state": "Enabled",
    "tenantId": "xxx-xxx-xxx"
  }
]
```

### Check Logs
The service logs subscription access. Check the workflow logs for:
- Successful fetch: Subscription dropdown populates with real data
- Permission errors: "Unable to load subscriptions (try signing out and in again)"
- Token acquisition errors in server logs

### Troubleshooting

**If subscriptions don't appear:**
1. Verify the Azure AD app has "Azure Service Management" API permission with `user_impersonation` scope
2. Ensure admin consent was granted for the permission
3. Sign out and sign back in to obtain new tokens with the Azure Resource Manager scope
4. Check that your Azure AD user has at least Reader role on Azure subscriptions
5. Check browser console and server logs for errors

## Future Enhancements

1. **Multi-tenant Support**: Allow users to switch between different Azure AD tenants
2. **Subscription Filtering**: Filter by subscription state (Enabled/Disabled)
3. **Resource Group Context**: Extend to resource group selection
4. **Cached Results**: Cache subscription list to reduce API calls
5. **User Token Authentication**: Use user's actual Azure AD token instead of service principal
