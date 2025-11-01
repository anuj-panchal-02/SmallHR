import React from 'react';
import AccessDenied from '../pages/AccessDenied';
import { useNotification } from '../contexts/NotificationContext';
import { useRolePermissions } from '../hooks/useRolePermissions';
import { Spin } from 'antd';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredPath: string;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, requiredPath }) => {
  const { canAccessPage, loading } = useRolePermissions();
  const notify = useNotification();

  // Show loading while checking permissions
  if (loading) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh',
        background: 'var(--gradient-surface)',
      }}>
        <Spin size="large" tip="Checking permissions..." />
      </div>
    );
  }

  // Check if user has access to this page
  if (!canAccessPage(requiredPath)) {
    // Gentle heads-up (non-blocking) then show a friendly 403 page
    try {
      notify.warning('Access denied', 'You don\'t have permission to view this page.');
    } catch {}
    return <AccessDenied requiredPath={requiredPath} />;
  }

  return <>{children}</>;
};

