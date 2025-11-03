import React, { useEffect, useState } from 'react';
import { Tooltip, Spin, Empty } from 'antd';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  DashboardOutlined,
  TeamOutlined,
  CalendarOutlined,
  DollarOutlined,
  BarChartOutlined,
  UserOutlined,
  BellOutlined,
  SettingOutlined,
  LogoutOutlined,
  SafetyOutlined,
  CaretDownOutlined,
  CaretRightOutlined,
  ApartmentOutlined,
} from '@ant-design/icons';
import { useAuthStore } from '../../store/authStore';
import { useRolePermissions } from '../../hooks/useRolePermissions';
import { useModulesStore } from '../../store/modulesStore';
import type { ModuleNode } from '../../services/modules';

interface MenuItem {
  key: string;
  icon: React.ReactNode;
  label: string;
  path: string;
  children?: MenuItem[];
}

interface MenuSection {
  title: string;
  items: MenuItem[];
  hasChildren?: boolean;
}

interface SidebarProps {
  collapsed: boolean;
}

export default function Sidebar({ collapsed }: SidebarProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const { logout, user } = useAuthStore();
  const { canAccessPage } = useRolePermissions();
  const [activeKey, setActiveKey] = useState(location.pathname);
  const [expandedKeys, setExpandedKeys] = useState<Set<string>>(new Set());

  const { modules, loading, refresh } = useModulesStore();
  const isSuperAdmin = user?.roles?.[0] === 'SuperAdmin';

  useEffect(() => {
    refresh();
  }, [refresh]);

  const pickIcon = (_name: string, path: string) => {
    const p = path.toLowerCase();
    if (p.includes('dashboard')) return <DashboardOutlined />;
    if (p.includes('employee')) return <UserOutlined />;
    if (p.includes('organization')) return <TeamOutlined />;
    if (p.includes('department')) return <TeamOutlined />;
    if (p.includes('position')) return <UserOutlined />;
    if (p.includes('calendar')) return <CalendarOutlined />;
    if (p.includes('payroll') || p.includes('expense')) return <DollarOutlined />;
    if (p.includes('notice') || p.includes('report')) return <BarChartOutlined />;
    if (p.includes('permission') || p.includes('role')) return <SafetyOutlined />;
    if (p.includes('tenant')) return <ApartmentOutlined />;
    return <BellOutlined />;
  };

  const flattenToSections = (nodes: ModuleNode[]): MenuSection[] => {
    // Convert first-level nodes to sections; preserve children structure
    return nodes.map(n => {
      const items: MenuItem[] = [];
      
      if (n.children && n.children.length > 0) {
        // Node has children - filter children based on permissions
        const accessibleChildren = n.children.filter(c => canAccessPage(c.path));
        
        // Only create parent item if there are accessible children
        if (accessibleChildren.length > 0) {
          const firstChild = accessibleChildren[0];
          items.push({
            key: n.path || firstChild.path,
            icon: pickIcon(n.name, n.path || firstChild.path),
            label: n.name,
            path: firstChild.path, // Navigate to first child when clicking parent
            children: accessibleChildren.map(c => ({
              key: c.path,
              icon: pickIcon(c.name, c.path),
              label: c.name,
              path: c.path,
            }))
          });
        }
      } else {
        // No children, just show the node if accessible
        if (canAccessPage(n.path)) {
          items.push({
            key: n.path,
            icon: pickIcon(n.name, n.path),
            label: n.name,
            path: n.path,
          });
        }
      }
      
      return {
        title: n.name,
        items: items,
        hasChildren: items.some(i => i.children && i.children.length > 0)
      };
    });
  };

  // Add SuperAdmin-specific menu items
  const getSuperAdminMenuItems = (): MenuItem[] => {
    if (!isSuperAdmin) return [];
    
    const items: MenuItem[] = [];
    
    // SuperAdmin always has access to admin endpoints, but check permissions for consistency
    if (canAccessPage('/admin/dashboard')) {
      items.push({
        key: '/admin/dashboard',
        icon: <DashboardOutlined />,
        label: 'SuperAdmin Dashboard',
        path: '/admin/dashboard',
      });
    }
    
    if (canAccessPage('/admin/tenants')) {
      items.push({
        key: '/admin/tenants',
        icon: <ApartmentOutlined />,
        label: 'Tenants',
        path: '/admin/tenants',
      });
    }
    
    if (canAccessPage('/admin/billing')) {
      items.push({
        key: '/admin/billing',
        icon: <DollarOutlined />,
        label: 'Billing Center',
        path: '/admin/billing',
      });
    }
    
    if (canAccessPage('/admin/alerts')) {
      items.push({
        key: '/admin/alerts',
        icon: <BellOutlined />,
        label: 'Alerts Hub',
        path: '/admin/alerts',
      });
    }
    
    if (canAccessPage('/admin/usage')) {
      items.push({
        key: '/admin/usage',
        icon: <BarChartOutlined />,
        label: 'Usage Dashboard',
        path: '/admin/usage',
      });
    }
    
    if (canAccessPage('/role-permissions')) {
      items.push({
        key: '/role-permissions',
        icon: <SafetyOutlined />,
        label: 'Role Permissions',
        path: '/role-permissions',
      });
    }
    
    return items;
  };

  // Add Admin-specific menu items based on permissions
  const getAdminMenuItems = (): MenuItem[] => {
    const isAdmin = user?.roles?.[0] === 'Admin';
    if (!isAdmin) return [];
    
    const items: MenuItem[] = [];
    
    // Only add Role Permissions if Admin has permission to access it
    if (canAccessPage('/role-permissions')) {
      items.push({
        key: '/role-permissions',
        icon: <SafetyOutlined />,
        label: 'Role Permissions',
        path: '/role-permissions',
      });
    }
    
    return items;
  };

  // Use modules from database (built from permissions)
  // SuperAdmin will get all pages from database, no fallback needed
  // Tenant Admin will get pages based on their initialized permissions
  const modulesToUse = modules;

  const regularMenuSections: MenuSection[] = flattenToSections(modulesToUse);
  
  // Add SuperAdmin section if user is SuperAdmin
  const superAdminMenuItems = getSuperAdminMenuItems();
  const adminMenuItems = getAdminMenuItems();
  let menuSections: MenuSection[] = regularMenuSections;
  
  if (isSuperAdmin && superAdminMenuItems.length > 0) {
    // Add SuperAdmin section at the beginning
    menuSections = [
      {
        title: 'Administration',
        items: superAdminMenuItems,
        hasChildren: false,
      },
      ...regularMenuSections,
    ];
  } else if (adminMenuItems.length > 0) {
    // Add Admin-specific menu items (e.g., Role Permissions)
    // Check if "Settings" section already exists in regularMenuSections
    const hasSettingsSection = regularMenuSections.some(s => 
      s.title.toLowerCase() === 'settings' || 
      s.items.some(i => i.path === '/settings')
    );
    
    // Use "Administration" title if Settings section already exists to avoid duplicates
    const adminSectionTitle = hasSettingsSection ? 'Administration' : 'Settings';
    
    menuSections = [
      {
        title: adminSectionTitle,
        items: adminMenuItems,
        hasChildren: false,
      },
      ...regularMenuSections,
    ];
  }

  const handleMenuClick = (path: string) => {
    setActiveKey(path);
    navigate(path);
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div
      style={{
        width: collapsed ? 80 : 280,
        height: '100vh',
        background: 'var(--color-sidebar)',
        display: 'flex',
        flexDirection: 'column',
        position: 'fixed',
        left: 0,
        top: 0,
        zIndex: 999,
        transition: 'width 250ms ease-in-out',
        borderRight: 'none',
      }}
    >
      {/* Logo Section */}
      <div
        style={{
          height: 64,
          padding: '0 12px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <img
            src="/hr-logo.png"
            alt="SmallHR Logo"
            style={{
              width: 28,
              height: 28,
              objectFit: 'contain',
            }}
          />
          {!collapsed && (
            <span
              style={{
                fontSize: 18,
                fontWeight: 700,
                background: 'var(--gradient-primary)',
                WebkitBackgroundClip: 'text',
                WebkitTextFillColor: 'transparent',
                letterSpacing: '-0.5px',
                color: 'var(--color-text-primary, #1E293B)',
              }}
            >
              SmallHR
            </span>
          )}
        </div>
      </div>

      {/* Navigation Menu - Scrollable Middle Section */}
      <div
        style={{
          flex: 1,
          overflowY: 'auto',
          overflowX: 'hidden',
          padding: collapsed ? '12px 8px' : '12px',
        }}
        className="custom-scrollbar"
      >
        {loading && (
          <div style={{ display: 'flex', justifyContent: 'center', marginTop: 16 }}>
            <Spin />
          </div>
        )}
        {!loading && menuSections.length === 0 && (
          <Empty description="No modules" />
        )}
        {menuSections.map((section, sectionIndex) => {
          // Don't render section if no accessible items (filtered in flattenToSections)
          if (section.items.length === 0) return null;

          // Use unique key combining section title and index to avoid duplicates
          const uniqueKey = `${section.title}-${sectionIndex}`;

          return (
          <div key={uniqueKey} style={{ marginBottom: collapsed ? 8 : 12 }}>
            {!collapsed && (
              <div style={{ marginBottom: 8, paddingLeft: 12 }}>
                <span
                  style={{
                    fontSize: 11,
                    fontWeight: 600,
                    color: 'var(--color-text-tertiary, #94A3B8)',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                    fontFamily: 'Inter, sans-serif',
                  }}
                >
                  {section.title}
                </span>
              </div>
            )}
            
            {collapsed && sectionIndex > 0 && (
              <div
                style={{
                  height: 1,
                  background: 'rgba(148, 163, 184, 0.2)',
                  margin: '8px 0',
                }}
              />
            )}

            {section.items.map((item, itemIndex) => {
              const isActive = activeKey === item.key;
              const isExpanded = expandedKeys.has(item.key);
              const hasChildren = item.children && item.children.length > 0;
              
              // Use unique key combining item key and index to avoid duplicates across sections
              const uniqueItemKey = `${item.key}-${sectionIndex}-${itemIndex}`;
              
              const menuItemContent = (
                <div key={uniqueItemKey}>
                  {/* Parent item */}
                  <div
                    onClick={(e) => {
                      if (hasChildren && !collapsed) {
                        e.stopPropagation();
                        setExpandedKeys(prev => {
                          const newSet = new Set(prev);
                          if (newSet.has(item.key)) {
                            newSet.delete(item.key);
                          } else {
                            newSet.add(item.key);
                          }
                          return newSet;
                        });
                      } else {
                        handleMenuClick(item.path);
                      }
                    }}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 12,
                      padding: collapsed ? '10px 8px' : '10px 12px',
                      marginBottom: 2,
                      borderRadius: 6,
                      cursor: 'pointer',
                      background: isActive ? 'var(--color-primary)' : 'transparent',
                      color: isActive ? '#FFFFFF' : 'var(--color-text-secondary, #64748B)',
                      transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
                      fontWeight: 500,
                      fontSize: 13,
                      boxShadow: isActive ? '0 2px 8px rgba(79, 70, 229, 0.3)' : 'none',
                      transform: isActive ? 'translateX(2px)' : 'translateX(0)',
                      justifyContent: collapsed ? 'center' : 'flex-start',
                      fontFamily: 'Inter, sans-serif',
                    }}
                    onMouseEnter={(e) => {
                      if (!isActive) {
                        e.currentTarget.style.background = 'var(--primary-08a)';
                        e.currentTarget.style.color = 'var(--color-primary)';
                        e.currentTarget.style.transform = 'translateX(2px)';
                      }
                    }}
                    onMouseLeave={(e) => {
                      if (!isActive) {
                        e.currentTarget.style.background = 'transparent';
                        e.currentTarget.style.color = 'var(--color-text-secondary, #64748B)';
                        e.currentTarget.style.transform = 'translateX(0)';
                      }
                    }}
                  >
                    <span style={{ fontSize: 16, display: 'flex', alignItems: 'center' }}>{item.icon}</span>
                    {!collapsed && (
                      <>
                        <span style={{ flex: 1 }}>{item.label}</span>
                        {hasChildren && (
                          <span style={{ fontSize: 10 }}>
                            {isExpanded ? <CaretDownOutlined /> : <CaretRightOutlined />}
                          </span>
                        )}
                      </>
                    )}
                  </div>
                  
                  {/* Children items */}
                  {hasChildren && !collapsed && isExpanded && item.children && (
                    <div style={{ marginLeft: 12, marginTop: 2 }}>
                      {item.children.map((child, childIndex) => {
                        const childIsActive = activeKey === child.key;
                        // Use unique key for children to avoid duplicates
                        const uniqueChildKey = `${child.key}-${sectionIndex}-${itemIndex}-${childIndex}`;
                        return (
                          <div
                            key={uniqueChildKey}
                            onClick={() => handleMenuClick(child.path)}
                            style={{
                              display: 'flex',
                              alignItems: 'center',
                              gap: 12,
                              padding: '8px 12px',
                              marginBottom: 2,
                              borderRadius: 6,
                              cursor: 'pointer',
                              background: childIsActive ? 'var(--color-primary)' : 'transparent',
                              color: childIsActive ? '#FFFFFF' : 'var(--color-text-secondary, #64748B)',
                              transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
                              fontWeight: 500,
                              fontSize: 13,
                              boxShadow: childIsActive ? '0 2px 8px rgba(79, 70, 229, 0.3)' : 'none',
                              fontFamily: 'Inter, sans-serif',
                            }}
                            onMouseEnter={(e) => {
                              if (!childIsActive) {
                                e.currentTarget.style.background = 'var(--primary-08a)';
                                e.currentTarget.style.color = 'var(--color-primary)';
                              }
                            }}
                            onMouseLeave={(e) => {
                              if (!childIsActive) {
                                e.currentTarget.style.background = 'transparent';
                                e.currentTarget.style.color = 'var(--color-text-secondary, #64748B)';
                              }
                            }}
                          >
                            <span style={{ fontSize: 14 }}>{child.icon}</span>
                            <span>{child.label}</span>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>
              );

              return collapsed ? (
                <Tooltip title={item.label} placement="right">
                  {menuItemContent}
                </Tooltip>
              ) : (
                menuItemContent
              );
            })}
          </div>
          );
        })}
      </div>

      {/* Bottom Action Buttons - Fixed at Bottom */}
      <div
        style={{
          padding: collapsed ? '12px 8px' : '12px',
          borderTop: '1px solid var(--color-border)',
          background: 'transparent',
        }}
      >
        {/* Notifications Button */}
        <Tooltip title={collapsed ? 'Notifications' : ''} placement="right">
          <div
            onClick={() => navigate('/notifications')}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 12,
              padding: collapsed ? '10px 8px' : '10px 12px',
              marginBottom: 4,
              borderRadius: 8,
              cursor: 'pointer',
              background: 'transparent',
              color: 'var(--color-text-secondary, #64748B)',
              transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
              fontWeight: 500,
              fontSize: 13,
              justifyContent: collapsed ? 'center' : 'flex-start',
              fontFamily: 'Inter, sans-serif',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.background = 'var(--primary-08a)';
              e.currentTarget.style.color = 'var(--color-primary)';
              e.currentTarget.style.transform = 'translateX(2px)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.background = 'transparent';
              e.currentTarget.style.color = 'var(--color-text-secondary, #64748B)';
              e.currentTarget.style.transform = 'translateX(0)';
            }}
          >
            <BellOutlined style={{ fontSize: 16, display: 'flex', alignItems: 'center' }} />
            {!collapsed && <span>Notifications</span>}
          </div>
        </Tooltip>

        {/* Settings Button */}
        <Tooltip title={collapsed ? 'Settings' : ''} placement="right">
          <div
            onClick={() => navigate('/settings')}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 12,
              padding: collapsed ? '10px 8px' : '10px 12px',
              marginBottom: 4,
              borderRadius: 8,
              cursor: 'pointer',
              background: 'transparent',
              color: 'var(--color-text-secondary, #64748B)',
              transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
              fontWeight: 500,
              fontSize: 13,
              justifyContent: collapsed ? 'center' : 'flex-start',
              fontFamily: 'Inter, sans-serif',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.background = 'var(--primary-08a)';
              e.currentTarget.style.color = 'var(--color-primary)';
              e.currentTarget.style.transform = 'translateX(2px)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.background = 'transparent';
              e.currentTarget.style.color = 'var(--color-text-secondary, #64748B)';
              e.currentTarget.style.transform = 'translateX(0)';
            }}
          >
            <SettingOutlined style={{ fontSize: 16, display: 'flex', alignItems: 'center' }} />
            {!collapsed && <span>Settings</span>}
          </div>
        </Tooltip>

        {/* Logout Button */}
        <Tooltip title={collapsed ? 'Logout' : ''} placement="right">
          <div
            onClick={handleLogout}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 12,
              padding: collapsed ? '10px 8px' : '10px 12px',
              borderRadius: 8,
              cursor: 'pointer',
              background: 'transparent',
              color: 'var(--color-error, #EF4444)',
              transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
              fontWeight: 500,
              fontSize: 13,
              justifyContent: collapsed ? 'center' : 'flex-start',
              fontFamily: 'Inter, sans-serif',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.background = 'rgba(239, 68, 68, 0.06)';
              e.currentTarget.style.color = 'var(--color-error, #DC2626)';
              e.currentTarget.style.transform = 'translateX(2px)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.background = 'transparent';
              e.currentTarget.style.color = 'var(--color-error, #EF4444)';
              e.currentTarget.style.transform = 'translateX(0)';
            }}
          >
            <LogoutOutlined style={{ fontSize: 16, display: 'flex', alignItems: 'center' }} />
            {!collapsed && <span>Logout</span>}
          </div>
        </Tooltip>
      </div>
    </div>
  );
}
