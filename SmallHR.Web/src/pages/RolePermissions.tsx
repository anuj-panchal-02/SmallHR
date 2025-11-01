import { useState, useEffect } from 'react';
import { Switch, Button, message, Tag, Space, Popconfirm, Alert, Checkbox, Empty, Divider } from 'antd';
import { SaveOutlined, ReloadOutlined, CheckCircleOutlined, PlusOutlined, FolderOutlined, FolderOpenOutlined } from '@ant-design/icons';
import api from '../services/api';
import { useModulesStore } from '../store/modulesStore';
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

  const fetchPermissions = async () => {
    setLoading(true);
    try {
      const response = await api.get('/rolepermissions');
      setPermissions(response.data);
      setChangedPermissions(new Set());
      // Auto-expand all modules initially
      const allModulePaths = getAllModulePaths(modules);
      setExpandedModules(allModulePaths);
    } catch (error: any) {
      if (error.response?.status === 404 || error.response?.data?.includes('no permissions')) {
        message.info('No permissions found. Click "Initialize Permissions" to set up default permissions.');
      } else {
        message.error('Failed to load permissions');
      }
      console.error(error);
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
      if (matches) {
        return { ...p, canAccess: checked } as RolePermission;
      }
      return p;
    });
    setPermissions(updated);
    const perms = updated.filter(p => {
      const matches = includeChildren 
        ? (p.pagePath === modulePath || p.pagePath.startsWith(modulePath + '/'))
        : p.pagePath === modulePath;
      return matches;
    });
    perms.forEach(p => markChanged(p.id));
  };

  const toggleSelectedAccess = (checked: boolean, includeChildren: boolean) => {
    selectedModules.forEach(modulePath => {
      toggleBulkAccess(modulePath, checked, includeChildren);
    });
    setSelectedModules(new Set());
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
      fetchPermissions();
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
      const modulePermissions = permissions.filter(p => p.pagePath === module.path);
      const children = module.children ? buildModuleGroups(module.children) : [];
      return {
        module,
        permissions: modulePermissions,
        children,
      };
    });
  };

  const moduleGroups = buildModuleGroups(modules);
  const roles = ['SuperAdmin', 'Admin', 'HR', 'Employee'];

  const renderPermissionSwitches = (path: string) => {
    return (
      <div style={{ display: 'flex', gap: 16, justifyContent: 'center' }}>
        {roles.map(role => {
          const perm = permissions.find(p => p.pagePath === path && p.roleName === role);
          if (!perm) return <div key={role} style={{ width: 90 }} />;
          
          const disabled = role === 'SuperAdmin';
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
            <Checkbox.Group
              options={roles.map(r => ({ label: r, value: r }))}
              value={selectedRoles}
              onChange={(vals) => setSelectedRoles(vals as string[])}
            />
            <Divider type="vertical" />
            <Button
              size="small"
              onClick={() => toggleSelectedAccess(true, false)}
              disabled={selectedRoles.length === 0}
            >
              Grant Access
            </Button>
            <Button
              size="small"
              danger
              onClick={() => toggleSelectedAccess(false, false)}
              disabled={selectedRoles.length === 0}
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
        ) : moduleGroups.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '40px 0' }}>
            <Empty 
              description="No modules found" 
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
