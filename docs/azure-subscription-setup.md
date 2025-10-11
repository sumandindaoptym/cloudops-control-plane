# Azure Subscription Integration

## Overview
The CloudOps platform integrates with Azure to fetch and display subscriptions that the logged-in user has access to. This allows users to select the appropriate subscription context for their operations.

## Current Implementation

### Backend
- **API Endpoint**: `GET /api/azure/subscriptions`
- **SDK**: Azure.Identity + Azure.ResourceManager (latest .NET SDK)
- **Authentication**: ClientSecretCredential using Azure AD app credentials
- **Fallback**: Returns mock subscriptions when no real subscriptions are accessible

### Frontend
- **Component**: `SubscriptionSelector.tsx`
- **Features**:
  - Fetches subscriptions from API
  - Displays in dropdown selector
  - Persists selection in localStorage
  - Shows loading state while fetching
  - Cloud icon for visual clarity

## Azure AD Permissions Required

To fetch real Azure subscriptions, the Azure AD app registration (service principal) needs proper permissions:

### 1. **Assign Reader Role**
The service principal must be granted **Reader** role on the subscriptions you want to access:

**Steps:**
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Subscriptions**
3. Select each subscription you want to access
4. Click **Access control (IAM)**
5. Click **+ Add** → **Add role assignment**
6. Select **Reader** role
7. In **Members** tab, select **Service principal**
8. Search for your app registration name
9. Click **Select** and **Review + assign**

### 2. **Verify App Registration**
Ensure your app registration has the following:

- **Client ID**: `AZURE_AD_CLIENT_ID` (already configured)
- **Client Secret**: `AZURE_AD_CLIENT_SECRET` (already configured)
- **Tenant ID**: `AZURE_AD_TENANT_ID` (already configured)

### 3. **API Permissions (Optional)**
If you need additional Azure Resource Manager API access, add these permissions:

1. Go to your app registration in Azure Portal
2. Navigate to **API permissions**
3. Click **+ Add a permission**
4. Select **Azure Service Management**
5. Add **user_impersonation** permission
6. Grant admin consent

## Development Mode

When the service principal lacks permissions or returns no subscriptions, the API automatically returns mock data:

```json
[
  {
    "id": "mock-prod",
    "name": "Production Subscription (Mock)",
    "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "tenantId": "...",
    "state": "Enabled"
  },
  {
    "id": "mock-dev",
    "name": "Development Subscription (Mock)",
    "subscriptionId": "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy",
    "tenantId": "...",
    "state": "Enabled"
  },
  {
    "id": "mock-staging",
    "name": "Staging Subscription (Mock)",
    "subscriptionId": "zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz",
    "tenantId": "...",
    "state": "Enabled"
  }
]
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
