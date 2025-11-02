import { useState, useEffect } from 'react';
import { Switch, Button, message, Tag, Space, Popconfirm, Alert, Checkbox, Empty, Divider } from 'antd';
import { SaveOutlined, ReloadOutlined, CheckCircleOutlined, PlusOutlined, FolderOutlined, FolderOpenOutlined } from '@ant-design/icons';
import api from '../services/api';
import { useModulesStore } from '../store/modulesStore';
import { useAuthStore } from '../store/authStore';
import type { ModuleNode } from '../services/modules';

interface RolePermission {
  id: number;
  roleName: string;
  pagePath: string;
  pageName: string;
  canAccess: boolean;
  canView: boolean;
  canCreate: boolean;
  canEdit: boolean;
  canDelete: boolean;
  description: string | null;
}

interface ModuleGroup {
  module: ModuleNode;
  permissions: RolePermission[];
  children: ModuleGroup[];
}

export default function RolePermissions() {
  const [permissions, setPermissions] = useState<RolePermission[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [changedPermissions, setChangedPermissions] = useState<Set<number>>(new Set());
  const [expandedModules, setExpandedModules] = useState<string[]>([]);
  const [selectedModules, setSelectedModules] = useState<Set<string>>(new Set());
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  
  const { modules, refresh } = useModulesStore();

  useEffect(() => {
    refresh();
  }, [refresh]);

  useEffect(() => {
    fetchPermissions();
  }, []);

  const { user } = useAuthStore();
  const isSuperAdmin = user?.roles?.[0] === 'SuperAdmin';
  
  // For SuperAdmin, show only Admin role. For others, show all roles
  const roles = isSuperAdmin ? ['Admin'] : ['SuperAdmin', 'Admin', 'HR', 'Employee'];

  // Build modules from permissions if modules are empty (for SuperAdmin)
  const getFallbackModulesFromPermissions = (permsToUse: RolePermission[]): ModuleNode[] => {
    if (modules.length > 0 || permsToUse.length === 0) return [];
    
    // Filter permissions based on role for fallback
    const filteredPerms = isSuperAdmin 
      ? permsToUse.filter(p => p.roleName === 'Admin')
      : permsToUse;
    
    if (filteredPerms.length === 0) return [];
    
    // Create a map of all page paths
    const pathMap = new Map<string, ModuleNode>();
    
    // First pass: create all nodes from permissions
    filteredPerms.forEach(perm => {
      if (!pathMap.has(perm.pagePath)) {
        pathMap.set(perm.pagePath, {
          name: perm.pageName,
          path: perm.pagePath,
        });
      }
    });
    
    // Second pass: organize parent/child relationships
    const childNodes: ModuleNode[] = [];
    
    pathMap.forEach((node, path) => {
      // Special handling for known parent/child relationships
      if (path === '/departments' || path === '/positions') {
        // These are children of /organization
        if (!pathMap.has('/organization')) {
          // Create organization parent if it doesn't exist
          pathMap.set('/organization', {
            name: 'Organization',
            path: '/organization',
          });
        }
        const org = pathMap.get('/organization');
        if (org) {
          org.children = org.children || [];
          org.children.push(node);
          childNodes.push(node);
        }
      } else if (path.startsWith('/payroll/')) {
        // Payroll children
        if (!pathMap.has('/payroll')) {
          pathMap.set('/payroll', {
            name: 'Payroll',
            path: '/payroll',
          });
        }
        const payroll = pathMap.get('/payroll');
        if (payroll) {
          payroll.children = payroll.children || [];
          payroll.children.push(node);
          childNodes.push(node);
        }
      }
    });
    
    // Third pass: organize remaining parent/child relationships
    const finalNodes: ModuleNode[] = [];
    pathMap.forEach((node, path) => {
      // Skip if already added as child
      if (childNodes.includes(node)) return;
      
      // Check if it has children
      const children = Array.from(pathMap.values()).filter(n => 
        n.path !== path && 
        n.path.startsWith(path + '/') &&
        n.path.split('/').filter(p => p).length === path.split('/').filter(p => p).length + 1
      );
      
      if (children.length > 0) {
        node.children = children;
        // Mark children as processed
        children.forEach(c => childNodes.push(c));
      }
      
      if (!childNodes.includes(node)) {
        finalNodes.push(node);
      }
    });
    
    return finalNodes;
  };

  const fetchPermissions = async () => {
    setLoading(true);
    try {
      const response = await api.get('/rolepermissions');
      const fetchedPermissions = Array.isArray(response.data) ? response.data : [];
      setPermissions(fetchedPermissions);
      setChangedPermissions(new Set());
      
      // Log for debugging
      if (fetchedPermissions.length === 0) {
        console.log('No permissions returned from API');
      } else {
        console.log(`Loaded ${fetchedPermissions.length} permissions`);
      }
      
      // Auto-expand all modules initially (after modules are available)
      setTimeout(() => {
        const modulesToUseForExpansion = modules.length === 0 && fetchedPermissions.length > 0
          ? getFallbackModulesFromPermissions(fetchedPermissions)
          : modules;
        const allModulePaths = getAllModulePaths(modulesToUseForExpansion);
        setExpandedModules(allModulePaths);
      }, 100);
    } catch (error: any) {
      if (error.response?.status === 404 || error.response?.data?.includes('no permissions')) {
        message.info(
          isSuperAdmin 
            ? 'No Admin permissions found. Click "Initialize Permissions" to create default Admin role permissions.'
            : 'No permissions found. Click "Initialize Permissions" to set up default permissions.'
        );
        setPermissions([]);
      } else {
        message.error('Failed to load permissions');
        console.error('Error fetching permissions:', error);
      }
    } finally {
      setLoading(false);
    }
  };

  const getAllModulePaths = (moduleNodes: ModuleNode[]): string[] => {
    const paths: string[] = [];
    moduleNodes.forEach(node => {
      paths.push(node.path);
      if (node.children) {
        paths.push(...getAllModulePaths(node.children));
      }
    });
    return paths;
  };

  // Filter permissions based on role - for SuperAdmin, show only Admin permissions
  const filteredPermissions = isSuperAdmin 
    ? permissions.filter(p => p.roleName === 'Admin')
    : permissions;

  const markChanged = (id: number) => setChangedPermissions(prev => new Set(prev).add(id));

  const toggleAccess = (path: string, role: string, checked: boolean) => {
    const updated = permissions.map(p => {
      if (p.pagePath === path && p.roleName === role) {
        return { ...p, canAccess: checked } as RolePermission;
      }
      return p;
    });
    setPermissions(updated);
    const perm = updated.find(p => p.pagePath === path && p.roleName === role);
    if (perm) markChanged(perm.id);
  };

  const toggleBulkAccess = (modulePath: string, checked: boolean, includeChildren: boolean) => {
    const updated = permissions.map(p => {
      const matches = includeChildren 
        ? (p.pagePath === modulePath || p.pagePath.startsWith(modulePath + '/'))
        : p.pagePath === modulePath;
      // Only update permissions for the allowed role (for SuperAdmin, only Admin)
      if (matches && (isSuperAdmin ? p.roleName === 'Admin' : true)) {
        return { ...p, canAccess: checked } as RolePermission;
      }
      return p;
    });
    setPermissions(updated);
    const perms = updated.filter(p => {
      const matches = includeChildren 
        ? (p.pagePath === modulePath || p.pagePath.startsWith(modulePath + '/'))
        : p.pagePath === modulePath;
      return matches && (isSuperAdmin ? p.roleName === 'Admin' : true);
    });
    perms.forEach(p => markChanged(p.id));
  };

  const toggleSelectedAccess = (checked: boolean, includeChildren: boolean) => {
    // For SuperAdmin, only allow changing Admin role permissions
    // Filter selectedRoles to only include Admin if SuperAdmin
    const rolesToUpdate = isSuperAdmin 
      ? selectedRoles.filter(r => r === 'Admin')
      : selectedRoles;
    
    if (rolesToUpdate.length === 0) {
      message.warning(isSuperAdmin ? 'Only Admin role can be modified' : 'Please select at least one role');
      return;
    }
    
    // Temporarily override selectedRoles for bulk update
    const originalSelectedRoles = selectedRoles;
    setSelectedRoles(rolesToUpdate);
    
    selectedModules.forEach(modulePath => {
      toggleBulkAccess(modulePath, checked, includeChildren);
    });
    
    setSelectedModules(new Set());
    setSelectedRoles(originalSelectedRoles);
  };

  const handleSaveChanges = async () => {
    setSaving(true);
    try {
      const changedItems = permissions.filter(p => changedPermissions.has(p.id));
      const updateDto = {
        permissions: changedItems.map(p => ({
          roleName: p.roleName,
          pagePath: p.pagePath,
          canAccess: p.canAccess,
          canView: p.canView,
          canCreate: p.canCreate,
          canEdit: p.canEdit,
          canDelete: p.canDelete,
        })),
      };

      await api.put('/rolepermissions/bulk-update', updateDto);

      message.success('Permissions updated successfully!');
      setChangedPermissions(new Set());
    } catch (error) {
      message.error('Failed to save permissions');
      console.error(error);
    } finally {
      setSaving(false);
    }
  };

  const handleInitialize = async () => {
    try {
      await api.post('/rolepermissions/initialize', null);
      message.success('Permissions initialized successfully!');
      
      // Refresh permissions in the page
      await fetchPermissions();
      
      // Refresh modules store to reload menu items from database
      refresh();
      
      // Refresh permissions in auth store so menu can check access
      const { fetchPermissions: fetchAuthPermissions } = useAuthStore.getState();
      await fetchAuthPermissions();
      
      message.info('Menu will refresh automatically...');
    } catch (error: any) {
      message.error(error.response?.data?.message || 'Failed to initialize permissions');
      console.error(error);
    }
  };

  const handleReset = async () => {
    try {
      await api.delete('/rolepermissions/reset');
      message.success('Permissions reset successfully!');
      setPermissions([]);
      setChangedPermissions(new Set());
    } catch (error) {
      message.error('Failed to reset permissions');
      console.error(error);
    }
  };

  const handleAddMissing = async () => {
    try {
      const response = await api.post('/rolepermissions/add-missing');
      message.success(response.data.message || 'Missing permissions added successfully!');
      fetchPermissions();
    } catch (error: any) {
      message.error(error.response?.data?.message || 'Failed to add missing permissions');
      console.error(error);
    }
  };

  const buildModuleGroups = (moduleNodes: ModuleNode[]): ModuleGroup[] => {
    return moduleNodes.map(module => {
      const modulePermissions = filteredPermissions.filter(p => p.pagePath === module.path);
      const children = module.children ? buildModuleGroups(module.children) : [];
      return {
        module,
        permissions: modulePermissions,
        children,
      };
    });
  };

  // Use fallback modules if modules are empty but permissions exist
  const modulesToUse = modules.length === 0 && filteredPermissions.length > 0
    ? getFallbackModulesFromPermissions(permissions)
    : modules;

  const moduleGroups = buildModuleGroups(modulesToUse);

  const renderPermissionSwitches = (path: string) => {
    return (
      <div style={{ display: 'flex', gap: 16, justifyContent: 'center' }}>
        {roles.map(role => {
          const perm = filteredPermissions.find(p => p.pagePath === path && p.roleName === role);
          if (!perm) return <div key={role} style={{ width: 90 }} />;
          
          // SuperAdmin can edit Admin permissions, so don't disable for SuperAdmin
          const disabled = !isSuperAdmin && role === 'SuperAdmin';
          const isChecked = perm.canAccess;

          return (
            <div key={role} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4, minWidth: 80 }}>
              <Tag
                color={
                  role === 'SuperAdmin' ? 'purple' :
                  role === 'Admin' ? 'red' :
                  role === 'HR' ? 'orange' : 'green'
                }
                style={{ fontSize: 11, fontWeight: 600, margin: 0 }}
              >
                {role}
              </Tag>
              <Switch
                checked={isChecked}
                onChange={(checked) => toggleAccess(path, role, checked)}
                disabled={disabled}
                size="small"
              />
            </div>
          );
        })}
      </div>
    );
  };

  const renderModuleGroup = (group: ModuleGroup, level: number = 0) => {
    const { module, children } = group;
    const hasChildren = children.length > 0;
    const isExpanded = expandedModules.includes(module.path);
    const isSelected = selectedModules.has(module.path);

    const indentLeft = level * 24;

    return (
      <div key={module.path} style={{ marginLeft: indentLeft, marginBottom: hasChildren ? 8 : 0 }}>
        {/* Module Header */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 12,
            padding: '12px 16px',
            marginBottom: 4,
            background: isSelected 
              ? 'var(--primary-08a, rgba(79, 70, 229, 0.1))' 
              : 'var(--glass-background)',
            borderRadius: 8,
            border: `1px solid ${isSelected ? 'var(--color-primary)' : 'var(--glass-border)'}`,
            cursor: 'pointer',
            transition: 'all 0.2s',
          }}
          onClick={(e) => {
            if (e.target instanceof HTMLInputElement) return;
            if (hasChildren) {
              setExpandedModules(prev => 
                prev.includes(module.path)
                  ? prev.filter(p => p !== module.path)
                  : [...prev, module.path]
              );
            }
            setSelectedModules(prev => {
              const newSet = new Set(prev);
              if (newSet.has(module.path)) {
                newSet.delete(module.path);
              } else {
                newSet.add(module.path);
              }
              return newSet;
            });
          }}
        >
          <Checkbox
            checked={isSelected}
            onClick={(e) => e.stopPropagation()}
            onChange={() => {
              setSelectedModules(prev => {
                const newSet = new Set(prev);
                if (newSet.has(module.path)) {
                  newSet.delete(module.path);
                } else {
                  newSet.add(module.path);
                }
                return newSet;
              });
            }}
          />

          {hasChildren && (
            <div style={{ fontSize: 14, color: 'var(--color-primary)' }}>
              {isExpanded ? <FolderOpenOutlined /> : <FolderOutlined />}
            </div>
          )}

          <div style={{ flex: 1, minWidth: 0 }}>
            <div style={{ 
              fontWeight: 600, 
              fontSize: 14, 
              color: 'var(--color-text-primary)', 
              marginBottom: 2 
            }}>
              {module.name}
            </div>
            <div style={{ 
              fontSize: 12, 
              color: 'var(--color-text-secondary)', 
              wordBreak: 'break-all' 
            }}>
              {module.path}
            </div>
          </div>

          {renderPermissionSwitches(module.path)}

          {/* Quick Actions */}
          <div style={{ display: 'flex', gap: 4 }}>
            <Button
              type="text"
              size="small"
              onClick={(e) => {
                e.stopPropagation();
                toggleBulkAccess(module.path, true, true);
              }}
              style={{ padding: '0 4px', fontSize: 11 }}
            >
              Allow All
            </Button>
            <Button
              type="text"
              size="small"
              danger
              onClick={(e) => {
                e.stopPropagation();
                toggleBulkAccess(module.path, false, true);
              }}
              style={{ padding: '0 4px', fontSize: 11 }}
            >
              Deny All
            </Button>
          </div>
        </div>

        {/* Children */}
        {hasChildren && isExpanded && (
          <div>
            {children.map(child => renderModuleGroup(child, level + 1))}
          </div>
        )}
      </div>
    );
  };

  return (
    <div>
      {/* Actions Bar */}
      <div style={{ marginBottom: 12 }}>
        <Space size="middle">
          {permissions.length === 0 && (
            <Button
              type="primary"
              icon={<CheckCircleOutlined />}
              onClick={handleInitialize}
              style={{
                borderRadius: 'var(--button-radius)',
                background: 'var(--gradient-success)',
                border: 'none',
              }}
            >
              Initialize Permissions
            </Button>
          )}
          {permissions.length > 0 && (
            <>
              <Button
                type="default"
                icon={<PlusOutlined />}
                onClick={handleAddMissing}
              >
                Add Missing
              </Button>
              <Button
                icon={<ReloadOutlined />}
                onClick={fetchPermissions}
              >
                Refresh
              </Button>
              <Popconfirm
                title="Reset all permissions?"
                description="This will delete all permissions. You'll need to initialize again."
                onConfirm={handleReset}
                okText="Yes, Reset"
                cancelText="Cancel"
                okButtonProps={{ danger: true }}
              >
                <Button danger>Reset All</Button>
              </Popconfirm>
              <Button
                type="primary"
                icon={<SaveOutlined />}
                onClick={handleSaveChanges}
                loading={saving}
                disabled={changedPermissions.size === 0}
                style={{
                  background: changedPermissions.size > 0 ? 'var(--gradient-primary)' : undefined,
                  border: 'none',
                }}
              >
                Save Changes ({changedPermissions.size})
              </Button>
            </>
          )}
        </Space>
      </div>

      {/* Unsaved Changes Warning */}
      {changedPermissions.size > 0 && (
        <Alert
          message={`${changedPermissions.size} unsaved changes`}
          type="warning"
          showIcon
          closable
          style={{ marginBottom: 12, borderRadius: 8 }}
        />
      )}

      {/* Bulk Selection Actions */}
      {selectedModules.size > 0 && (
        <div
          style={{
            marginBottom: 12,
            padding: 12,
          }}
        >
          <Space size="middle">
            <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--color-primary)' }}>
              {selectedModules.size} module(s) selected
            </div>
            {isSuperAdmin ? (
              <div style={{ fontSize: 13, color: 'var(--color-text-secondary)' }}>
                Managing: <Tag color="red">Admin</Tag> role for all tenants
              </div>
            ) : (
              <Checkbox.Group
                options={roles.map(r => ({ label: r, value: r }))}
                value={selectedRoles}
                onChange={(vals) => setSelectedRoles(vals as string[])}
              />
            )}
            <Divider type="vertical" />
            <Button
              size="small"
              onClick={() => {
                // For SuperAdmin, auto-set Admin role
                if (isSuperAdmin && selectedRoles.length === 0) {
                  setSelectedRoles(['Admin']);
                }
                toggleSelectedAccess(true, false);
              }}
              disabled={!isSuperAdmin && selectedRoles.length === 0}
            >
              Grant Access
            </Button>
            <Button
              size="small"
              danger
              onClick={() => {
                // For SuperAdmin, auto-set Admin role
                if (isSuperAdmin && selectedRoles.length === 0) {
                  setSelectedRoles(['Admin']);
                }
                toggleSelectedAccess(false, false);
              }}
              disabled={!isSuperAdmin && selectedRoles.length === 0}
            >
              Revoke Access
            </Button>
            <Button onClick={() => setSelectedModules(new Set())}>
              Clear Selection
            </Button>
          </Space>
        </div>
      )}

      {/* Permissions Display */}
      <div>
        {loading ? (
          <div style={{ textAlign: 'center', padding: '40px 0' }}>
            <Empty description="Loading permissions..." />
          </div>
        ) : filteredPermissions.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '40px 0' }}>
            <Empty 
              description={
                isSuperAdmin 
                  ? "No Admin permissions found. Click 'Initialize Permissions' to create default Admin role permissions."
                  : "No permissions found. Click 'Initialize Permissions' to set up default permissions."
              }
              image={Empty.PRESENTED_IMAGE_SIMPLE}
            />
          </div>
        ) : moduleGroups.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '40px 0' }}>
            <Empty 
              description="No modules found. Permissions exist but couldn't be organized into modules." 
              image={Empty.PRESENTED_IMAGE_SIMPLE}
            />
          </div>
        ) : (
          <div>
            <div style={{ 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'center',
              gap: 16,
              padding: '12px 16px',
              marginBottom: 16,
              background: 'var(--glass-background)',
              borderRadius: 8,
              border: '1px solid var(--glass-border)'
            }}>
              <div style={{ 
                fontSize: 12, 
                fontWeight: 600, 
                color: 'var(--color-text-secondary)',
                textTransform: 'uppercase',
                letterSpacing: '0.5px'
              }}>
                Page Access
              </div>
              {roles.map(role => (
                <Tag
                  key={role}
                  color={
                    role === 'SuperAdmin' ? 'purple' :
                    role === 'Admin' ? 'red' :
                    role === 'HR' ? 'orange' : 'green'
                  }
                  style={{ fontSize: 11, fontWeight: 600 }}
                >
                  {role}
                </Tag>
              ))}
              <div style={{ 
                fontSize: 12, 
                fontWeight: 600, 
                color: 'var(--color-text-secondary)',
                textTransform: 'uppercase',
                letterSpacing: '0.5px'
              }}>
                Actions
              </div>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              {moduleGroups.map(group => renderModuleGroup(group))}
            </div>
          </div>
        )}
      </div>

      {/* Help Section */}
      <div style={{ marginTop: 12 }}>
        <div style={{ fontSize: 13, color: 'var(--color-text-secondary)', lineHeight: 1.6 }}>
          <strong style={{ color: 'var(--color-text-primary)', display: 'block', marginBottom: 8 }}>
            How to use:
          </strong>
          <ul style={{ marginLeft: 20, marginBottom: 0 }}>
            <li>Click on modules to expand/collapse and see child pages</li>
            <li>Select modules to apply bulk actions to multiple pages</li>
            <li>Use "Allow All" / "Deny All" for quick parent + children updates</li>
            <li>SuperAdmin automatically has access to everything</li>
            <li>Don't forget to click "Save Changes" to apply updates</li>
          </ul>
        </div>
      </div>
    </div>
  );
}
