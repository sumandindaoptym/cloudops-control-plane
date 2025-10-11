# Azure Subscription Integration

## Overview
The CloudOps platform integrates with Azure to fetch and display subscriptions that the **logged-in user** has access to. This allows users to select the appropriate subscription context for their operations based on their own Azure RBAC permissions.

## Current Implementation

### Backend
- **API Endpoint**: `GET /api/azure/subscriptions`
- **SDK**: Azure.Identity + Azure.ResourceManager (latest .NET SDK)
- **Authentication**: User's Azure AD access token (delegated permissions)
- **Custom Credential**: `AccessTokenCredential` class to wrap user's token

### Frontend
- **Component**: `SubscriptionSelector.tsx`
- **Features**:
  - Fetches subscriptions from API
  - Displays in dropdown selector
  - Persists selection in localStorage
  - Shows loading state while fetching
  - Cloud icon for visual clarity

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

### 3. **NextAuth Configuration**
The auth configuration requests the Azure Resource Manager scope with refresh token support:

```typescript
scope: 'openid profile email User.Read https://management.azure.com/user_impersonation offline_access'
prompt: 'consent'
```

**Scopes Explained:**
- `openid profile email`: Basic user info
- `User.Read`: Microsoft Graph API access
- `https://management.azure.com/user_impersonation`: Azure Resource Manager delegated access
- `offline_access`: Enables refresh tokens for automatic token renewal

### 4. **Token Refresh**
The implementation includes automatic token refresh:
- Access tokens are automatically refreshed before expiration
- No user interruption when tokens expire
- Graceful fallback: redirects to sign-in if refresh fails
- Refresh tokens stored securely in JWT session

## How It Works

### Authentication Flow
1. User signs in with Microsoft (Azure AD)
2. NextAuth requests access token with Azure Resource Manager scope
3. Access token is stored in user's session
4. Frontend fetches session and passes access token to backend API
5. Backend uses user's token to call Azure Resource Manager API
6. API returns subscriptions the user has access to based on their RBAC permissions

### Token Flow Diagram
```
User Browser → NextAuth (Azure AD) → Access Token (with ARM scope)
                                              ↓
Frontend (session.accessToken) → Backend API (Authorization: Bearer <token>)
                                              ↓
                            Azure Resource Manager API
                                              ↓
                            Subscriptions (filtered by user's RBAC)
```

## Testing

### Test API Endpoint
```bash
curl http://localhost:5056/api/azure/subscriptions
```

### Expected Response (with permissions)
```json
[
  {
    "id": "/subscriptions/xxx",
    "name": "Your Subscription Name",
    "subscriptionId": "xxx-xxx-xxx",
    "tenantId": "xxx-xxx-xxx",
    "state": "Enabled"
  }
]
```

### Check Logs
The API logs subscription access:
```
[INF] Retrieved 3 Azure subscriptions
```

Or if permissions are missing:
```
[WRN] No Azure subscriptions found. The service principal may not have access to any subscriptions. Using mock data for development.
[INF] Retrieved 3 Azure subscriptions
```

## Future Enhancements

1. **Multi-tenant Support**: Allow users to switch between different Azure AD tenants
2. **Subscription Filtering**: Filter by subscription state (Enabled/Disabled)
3. **Resource Group Context**: Extend to resource group selection
4. **Cached Results**: Cache subscription list to reduce API calls
5. **User Token Authentication**: Use user's actual Azure AD token instead of service principal
