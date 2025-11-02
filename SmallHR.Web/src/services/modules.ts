import axios from 'axios';

const API_BASE_URL = 'http://localhost:5192/api';

export interface ModuleNode {
  name: string;
  path: string;
  description?: string | null;
  children?: ModuleNode[];
}

interface RolePermissionDto {
  roleName: string;
  pagePath: string;
  pageName: string;
  canAccess: boolean;
  description: string | null;
}

function buildTree(items: RolePermissionDto[]): ModuleNode[] {
  const byPath = new Map<string, ModuleNode>();
  const roots: ModuleNode[] = [];

  // Deduplicate items by pagePath - if same path appears multiple times, use the first one
  const uniqueItems = new Map<string, RolePermissionDto>();
  for (const it of items) {
    if (!it.canAccess) continue;
    // Only add if we haven't seen this path before (deduplication)
    if (!uniqueItems.has(it.pagePath)) {
      uniqueItems.set(it.pagePath, it);
    }
  }

  // Create nodes for accessible items (now deduplicated)
  for (const it of uniqueItems.values()) {
    byPath.set(it.pagePath, {
      name: it.pageName,
      path: it.pagePath,
      description: it.description,
      children: [],
    });
  }

  // Special handling: Ensure Organization parent exists with Departments and Positions as children
  // /departments and /positions should be children of /organization
  const hasDepartments = byPath.has('/departments');
  const hasPositions = byPath.has('/positions');
  const hasOrganization = byPath.has('/organization');

  if ((hasDepartments || hasPositions) && !hasOrganization) {
    // Create Organization parent if it doesn't exist but has children
    byPath.set('/organization', {
      name: 'Organization',
      path: '/organization',
      description: 'Organization structure management',
      children: [],
    });
  }

  // Link children by path prefix (e.g., /payroll/reports child of /payroll)
  // Also handle Organization -> Departments/Positions hierarchy
  const processedChildren = new Set<string>();
  
  for (const node of byPath.values()) {
    // Special case: Departments and Positions are children of Organization
    if (node.path === '/departments' || node.path === '/positions') {
      const orgNode = byPath.get('/organization');
      if (orgNode) {
        orgNode.children!.push(node);
        processedChildren.add(node.path);
        continue; // Don't process as root or regular parent/child
      }
    }

    // Regular parent/child linking by path prefix (skip if already processed as Organization child)
    if (!processedChildren.has(node.path)) {
      const parentPath = node.path.split('/').slice(0, -1).join('/') || '/';
      if (parentPath !== '/' && byPath.has(parentPath)) {
        byPath.get(parentPath)!.children!.push(node);
        processedChildren.add(node.path);
      }
    }
  }

  // Add all nodes that weren't processed as children to roots
  for (const node of byPath.values()) {
    if (!processedChildren.has(node.path)) {
      roots.push(node);
    }
  }

  // Sort children by name for consistency
  const sortTree = (nodes: ModuleNode[]) => {
    nodes.sort((a, b) => {
      // Sort Organization children: Departments before Positions
      if (a.path === '/departments' && b.path === '/positions') return -1;
      if (a.path === '/positions' && b.path === '/departments') return 1;
      return a.name.localeCompare(b.name);
    });
    nodes.forEach(n => n.children && sortTree(n.children));
  };
  sortTree(roots);
  return roots;
}

export async function fetchModulesForCurrentUser(): Promise<ModuleNode[]> {
  // Token is now sent via httpOnly cookie automatically
  const axiosConfig = { withCredentials: true };
  
  // Always use permissions from database to build modules dynamically
  // This ensures menu reflects actual permissions set in the database
  try {
    // Get user's own permissions from the database
    const res = await axios.get(`${API_BASE_URL}/rolepermissions/my-permissions`, axiosConfig);
    const permissions: RolePermissionDto[] = res.data;
    
    // Filter to only include permissions where canAccess = true
    const accessiblePermissions = permissions.filter(p => p.canAccess);
    
    // Build module tree from accessible permissions
    return buildTree(accessiblePermissions);
  } catch (e) {
    console.error('Failed to fetch permissions for modules:', e);
    // If my-permissions fails, try the general permissions endpoint
    try {
      const res = await axios.get(`${API_BASE_URL}/rolepermissions`, axiosConfig);
      const all: RolePermissionDto[] = res.data;
      
      // Get user role from localStorage (should be available)
      const userRaw = localStorage.getItem('user');
      const user = userRaw ? JSON.parse(userRaw) : undefined;
      const role = user?.roles?.[0];
      
      // Filter by user's role and only accessible permissions
      const filtered = role 
        ? all.filter(p => p.roleName === role && p.canAccess) 
        : all.filter(p => p.canAccess);
      
      return buildTree(filtered);
    } catch (fallbackError) {
      console.error('Fallback permission fetch also failed:', fallbackError);
      // Return empty array if both attempts fail
      return [];
    }
  }
}


