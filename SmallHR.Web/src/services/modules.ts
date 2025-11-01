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

  // Create nodes for accessible items
  for (const it of items) {
    if (!it.canAccess) continue;
    byPath.set(it.pagePath, {
      name: it.pageName,
      path: it.pagePath,
      description: it.description,
      children: [],
    });
  }

  // Link children by path prefix (e.g., /payroll/reports child of /payroll)
  for (const node of byPath.values()) {
    const parentPath = node.path.split('/').slice(0, -1).join('/') || '/';
    if (parentPath !== '/' && byPath.has(parentPath)) {
      byPath.get(parentPath)!.children!.push(node);
    } else {
      roots.push(node);
    }
  }

  // Sort children by name for consistency
  const sortTree = (nodes: ModuleNode[]) => {
    nodes.sort((a, b) => a.name.localeCompare(b.name));
    nodes.forEach(n => n.children && sortTree(n.children));
  };
  sortTree(roots);
  return roots;
}

export async function fetchModulesForCurrentUser(): Promise<ModuleNode[]> {
  // Token is now sent via httpOnly cookie automatically
  const axiosConfig = { withCredentials: true };
  
  try {
    const res = await axios.get(`${API_BASE_URL}/modules`, axiosConfig);
    // API returns already hierarchical nodes compatible with ModuleNode
    return res.data as ModuleNode[];
  } catch (e) {
    // Fallback to RolePermissions-derived tree
    const res = await axios.get(`${API_BASE_URL}/rolepermissions`, axiosConfig);
    const all: RolePermissionDto[] = res.data;
    const userRaw = localStorage.getItem('user');
    const user = userRaw ? JSON.parse(userRaw) : undefined;
    const role = user?.roles?.[0];
    const filtered = role ? all.filter(p => p.roleName === role) : all;
    return buildTree(filtered);
  }
}


