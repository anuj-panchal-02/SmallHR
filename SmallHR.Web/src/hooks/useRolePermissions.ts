import { useAuthStore } from '../store/authStore';

export const useRolePermissions = () => {
  const { user, permissions, permissionsLoaded } = useAuthStore();

  const canAccessPage = (pagePath: string): boolean => {
    // SuperAdmin always has access to everything
    if (user?.roles?.[0] === 'SuperAdmin') {
      return true;
    }

    // If permissions not loaded yet, wait
    if (!permissionsLoaded) {
      return false;
    }

    // If no permissions found, allow access to dashboard only (fallback)
    if (permissions.length === 0) {
      console.warn(`[RBAC] No permissions found for ${user?.roles?.[0]}. Allowing dashboard access only.`);
      return pagePath === '/dashboard';
    }

    // Find permission for this page
    const permission = permissions.find(p => p.pagePath === pagePath);
    const hasAccess = permission?.canAccess ?? false;
    
    if (!hasAccess) {
      console.log(`[RBAC] Access denied to ${pagePath} for role ${user?.roles?.[0]}`);
    }
    
    return hasAccess;
  };

  const getAccessiblePages = (): string[] => {
    if (user?.roles?.[0] === 'SuperAdmin') {
      return ['all']; // SuperAdmin has access to all
    }

    return permissions
      .filter(p => p.canAccess)
      .map(p => p.pagePath);
  };

  type Action = 'view' | 'create' | 'edit' | 'delete';
  const canPerformAction = (pagePath: string, action: Action): boolean => {
    // SuperAdmin shortcut
    if (user?.roles?.[0] === 'SuperAdmin') {
      return true;
    }
    if (!permissionsLoaded) return false;

    const permission = permissions.find(p => p.pagePath === pagePath);
    if (!permission) return false;

    switch (action) {
      case 'view':
        return permission.canView || permission.canAccess; // fallback to page access
      case 'create':
        return !!permission.canCreate;
      case 'edit':
        return !!permission.canEdit;
      case 'delete':
        return !!permission.canDelete;
      default:
        return false;
    }
  };

  return {
    permissions,
    loading: !permissionsLoaded,
    canAccessPage,
    getAccessiblePages,
    canPerformAction,
  };
};

