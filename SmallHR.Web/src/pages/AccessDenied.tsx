import { Button, Typography } from 'antd';
import { LockOutlined, ArrowLeftOutlined, SafetyOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useRolePermissions } from '../hooks/useRolePermissions';
import { useAuthStore } from '../store/authStore';

const { Title, Paragraph, Text } = Typography;

export default function AccessDenied({ requiredPath }: { requiredPath?: string }) {
  const navigate = useNavigate();
  const { canAccessPage } = useRolePermissions();
  const { user } = useAuthStore();

  const goHome = () => navigate('/dashboard');
  const goRolePermissions = () => navigate('/role-permissions');

  const canManagePermissions = canAccessPage('/role-permissions');

  return (
    <div
      style={{
        minHeight: '60vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '48px 24px',
      }}
    >
      <div
        className="glass-card"
        style={{
          maxWidth: 720,
          width: '100%',
          padding: 32,
          textAlign: 'center',
        }}
      >
        <div
          style={{
            width: 64,
            height: 64,
            margin: '0 auto 16px',
            borderRadius: 16,
            background: 'rgba(239, 68, 68, 0.12)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'var(--color-error, #EF4444)',
          }}
        >
          <LockOutlined style={{ fontSize: 28 }} />
        </div>

        <Title level={2} style={{ marginBottom: 8 }}>Access denied</Title>
        <Paragraph type="secondary" style={{ marginBottom: 16 }}>
          {requiredPath ? (
            <>
              You don’t have permission to view <Text code>{requiredPath}</Text>.
            </>
          ) : (
            <>You don’t have permission to view this page.</>
          )}
        </Paragraph>

        <Paragraph style={{ marginBottom: 24 }}>
          Signed in as <Text strong>{user?.email}</Text>. If you need access, contact your administrator.
        </Paragraph>

        <div style={{ display: 'flex', gap: 12, justifyContent: 'center', flexWrap: 'wrap' }}>
          <Button
            onClick={goHome}
            icon={<ArrowLeftOutlined />}
            style={{
              borderRadius: 'var(--button-radius)',
              height: 'var(--button-height)',
            }}
          >
            Back to dashboard
          </Button>

          {canManagePermissions && (
            <Button
              type="primary"
              onClick={goRolePermissions}
              icon={<SafetyOutlined />}
              style={{
                borderRadius: 'var(--button-radius)',
                height: 'var(--button-height)',
                background: 'var(--gradient-primary)',
                border: 'none',
                boxShadow: 'var(--button-shadow-primary)',
              }}
            >
              Manage role permissions
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}


