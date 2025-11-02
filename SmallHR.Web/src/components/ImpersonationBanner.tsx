import { useState, useEffect } from 'react';
import { Alert, Button, Space } from 'antd';
import { UserSwitchOutlined, CloseOutlined } from '@ant-design/icons';
import api from '../services/api';
import { useNotification } from '../contexts/NotificationContext';

interface ImpersonationBannerProps {
  tenantId?: number; // Optional - not used in component but useful for tracking
  tenantName: string;
  expiresAt: string;
  onStopImpersonation: () => void;
}

export default function ImpersonationBanner({
  tenantId: _tenantId, // Unused but kept for future use
  tenantName,
  expiresAt,
  onStopImpersonation,
}: ImpersonationBannerProps) {
  const notify = useNotification();
  const [minutesRemaining, setMinutesRemaining] = useState(0);

  useEffect(() => {
    const calculateRemaining = () => {
      const expiry = new Date(expiresAt);
      const now = new Date();
      const diff = expiry.getTime() - now.getTime();
      const minutes = Math.max(0, Math.floor(diff / 60000));
      setMinutesRemaining(minutes);
    };

    calculateRemaining();
    const interval = setInterval(calculateRemaining, 60000); // Update every minute

    return () => clearInterval(interval);
  }, [expiresAt]);

  const handleStopImpersonation = async () => {
    try {
      await api.post('/admin/tenants/stop-impersonation');
      notify.success('Impersonation Stopped', 'You are now back to SuperAdmin view.');
      onStopImpersonation();
    } catch (error: any) {
      notify.error('Error', 'Failed to stop impersonation. Please try again.');
      console.error('Stop impersonation error:', error);
    }
  };

  if (minutesRemaining <= 0) {
    return null;
  }

  return (
    <Alert
      message={
        <Space>
          <UserSwitchOutlined />
          <span>
            <strong>You're viewing as Tenant: {tenantName}</strong>
            {' '}(Impersonation expires in {minutesRemaining} minute{minutesRemaining !== 1 ? 's' : ''})
          </span>
        </Space>
      }
      type="warning"
      icon={<UserSwitchOutlined />}
      action={
        <Button
          size="small"
          danger
          icon={<CloseOutlined />}
          onClick={handleStopImpersonation}
        >
          Stop Impersonation
        </Button>
      }
      showIcon
      style={{
        marginBottom: 16,
        borderRadius: 4,
      }}
    />
  );
}

