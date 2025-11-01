# Session Management Guide

## Overview

The SmallHR application now uses a centralized session management system that stores user authentication data and permissions in a persistent Zustand store. This eliminates redundant API calls and provides a single source of truth for user session data.

## Architecture

### Session Store (`authStore.ts`)

The auth store maintains the following session data:

```typescript
interface AuthState {
  user: User | null;                    // Current user information
  token: string | null;                 // JWT access token
  refreshToken: string | null;          // Refresh token for token renewal
  isAuthenticated: boolean;             // Authentication status
  permissions: RolePermission[];        // User's role-based permissions
  permissionsLoaded: boolean;           // Flag indicating if permissions have been fetched
}
```

### Key Features

1. **Persistent Storage**: Session data is persisted to localStorage via Zustand's persist middleware
2. **Automatic Permission Loading**: Permissions are fetched automatically during login
3. **Single Source of Truth**: All components access the same session data
4. **Optimized Performance**: Permissions are fetched once and reused throughout the app

## How It Works

### Login Flow

```
User Login
    ↓
API Login Request (/auth/login)
    ↓
Store Token & User Data
    ↓
Fetch Permissions (/api/RolePermissions/my-permissions)
    ↓
Store Permissions in Session
    ↓
Navigate to Dashboard
```

### Permission Checking

```
Component Needs Permission Check
    ↓
Call useRolePermissions() Hook
    ↓
Read Permissions from Session Store
    ↓
Check Access (No API Call Needed!)
```

## Usage

### 1. Login Component

```typescript
import { useAuthStore } from '../store/authStore';

const { login } = useAuthStore();

// During login - await to ensure permissions are loaded
await login(token, refreshToken, user);
```

### 2. Permission Checking

```typescript
import { useRolePermissions } from '../hooks/useRolePermissions';

const { permissions, loading, canAccessPage } = useRolePermissions();

// Check if user can access a specific page
const hasAccess = canAccessPage('/employees');

// Get all accessible pages
const accessiblePages = getAccessiblePages();
```

### 3. Logout

```typescript
import { useAuthStore } from '../store/authStore';

const { logout } = useAuthStore();

// Clear all session data
logout();
```

### 4. Access User Data Anywhere

```typescript
import { useAuthStore } from '../store/authStore';

const { user, isAuthenticated, permissions } = useAuthStore();

if (isAuthenticated) {
  console.log('Current user:', user?.firstName);
  console.log('User role:', user?.roles[0]);
  console.log('Permissions:', permissions);
}
```

## API Endpoints

### Backend Controller (`RolePermissionsController.cs`)

#### GET `/api/RolePermissions/my-permissions`
- **Auth Required**: Yes (Bearer token)
- **Authorization**: Any authenticated user
- **Description**: Fetches permissions for the authenticated user's role (extracted from JWT token)
- **Returns**: Array of `RolePermissionDto`

```csharp
[HttpGet("my-permissions")]
public async Task<ActionResult<IEnumerable<RolePermissionDto>>> GetMyPermissions()
{
    // Role is automatically extracted from JWT token claims
    var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
    // ... fetch and return permissions
}
```

#### GET `/api/RolePermissions/role/{roleName}`
- **Auth Required**: Yes (Bearer token)
- **Authorization**: SuperAdmin only
- **Description**: Fetches permissions for any specific role (admin tool)

## Security Benefits

✅ **Token-Based Authentication**: User role is extracted from JWT token, not from user input  
✅ **No Role Spoofing**: Users can only fetch their own role's permissions  
✅ **Centralized Control**: All permissions managed through a single source  
✅ **Secure Endpoints**: Role-specific endpoints are properly protected with `[Authorize]` attributes  

## Session Persistence

Session data is automatically persisted to `localStorage` with the key `auth-storage`. This means:

- Users stay logged in across page refreshes
- Permissions are cached and don't need to be refetched
- Session survives browser restarts (until logout or token expiration)

### Persisted Data

The following fields are persisted:
- `user`
- `token`
- `refreshToken`
- `isAuthenticated`
- `permissions`
- `permissionsLoaded`

## Refresh Session Data

If you need to manually refresh permissions (e.g., after role change):

```typescript
import { useAuthStore } from '../store/authStore';

const { fetchPermissions } = useAuthStore();

// Manually refetch permissions
await fetchPermissions();
```

## Troubleshooting

### Permissions Not Loading

1. **Check Browser Console**: Look for `[Session]` prefixed logs
2. **Verify Token**: Ensure JWT token is valid and not expired
3. **Check Role**: User must have a role assigned in the JWT claims
4. **API Status**: Verify the API server is running on `http://localhost:5192`

### Permission Denied Errors

1. **Check Role**: Verify the user's role has the required permissions in the database
2. **SuperAdmin**: SuperAdmin role has access to everything by default
3. **Initialize Permissions**: Run the permission initialization endpoint if needed

### Session Lost After Refresh

1. **Check localStorage**: Verify `auth-storage` exists in browser localStorage
2. **Token Expiration**: JWT tokens expire after 60 minutes by default
3. **Use Refresh Token**: Implement token refresh logic if needed

## Best Practices

1. **Always await login()**: Ensure permissions are loaded before navigation
   ```typescript
   await login(token, refreshToken, user);
   ```

2. **Check loading state**: Wait for permissions to load before checking access
   ```typescript
   if (loading) return <Spinner />;
   ```

3. **Use SuperAdmin wisely**: SuperAdmin bypasses all permission checks
   ```typescript
   if (user?.roles?.[0] === 'SuperAdmin') return true;
   ```

4. **Logout on token expiration**: Clear session when tokens expire
   ```typescript
   if (tokenExpired) logout();
   ```

## Future Enhancements

- [ ] Implement automatic token refresh
- [ ] Add session timeout warnings
- [ ] Support for multiple roles per user
- [ ] Permission caching with TTL
- [ ] Session activity tracking
- [ ] Audit log for permission changes

## Related Files

- `SmallHR.Web/src/store/authStore.ts` - Session store implementation
- `SmallHR.Web/src/hooks/useRolePermissions.ts` - Permission checking hook
- `SmallHR.Web/src/types/api.ts` - TypeScript type definitions
- `SmallHR.API/Controllers/RolePermissionsController.cs` - Backend permissions API
- `SmallHR.API/Services/AuthService.cs` - Authentication service

## Support

For issues or questions about session management, check:
1. Browser console for error messages
2. API logs for authentication failures
3. Database for permission records
4. This guide for troubleshooting steps

