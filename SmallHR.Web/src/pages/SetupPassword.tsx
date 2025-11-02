import { useState, useEffect } from 'react';
import { Form, Input, Button, Typography, message } from 'antd';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { authAPI } from '../services/api';
import { 
  LockOutlined, 
  ArrowLeftOutlined,
  CheckCircleOutlined
} from '@ant-design/icons';
import './Login.css';

const { Title, Text } = Typography;

export default function SetupPassword() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [token, setToken] = useState<string | null>(null);
  const [userId, setUserId] = useState<string | null>(null);

  useEffect(() => {
    const tokenParam = searchParams.get('token');
    const userIdParam = searchParams.get('userId');
    
    if (!tokenParam || !userIdParam) {
      message.error('Invalid setup link. Missing token or userId.');
      navigate('/login');
      return;
    }
    
    setToken(tokenParam);
    setUserId(userIdParam);
  }, [searchParams, navigate]);

  const onFinish = async (values: { newPassword: string; confirmPassword: string }) => {
    if (!token || !userId) {
      message.error('Invalid setup token');
      navigate('/login');
      return;
    }

    setLoading(true);
    try {
      // Call the setup-password endpoint with userId, token, and newPassword
      // Token is already decoded from URL search params
      if (!token || !userId) {
        message.error('Invalid setup token');
        return;
      }
      
      await authAPI.setupPassword({
        userId: userId,
        token: token,
        newPassword: values.newPassword
      });
      
      setSuccess(true);
      message.success('Password set successfully! You can now login.');
      
      // Redirect to login after 2 seconds
      setTimeout(() => {
        navigate('/login');
      }, 2000);
    } catch (error: any) {
      console.error('Password setup failed:', error);
      message.error(error.response?.data?.message || 'Failed to set password. The link may have expired.');
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <div className="login-container">
        <div className="form-section">
          <div className="form-wrapper">
            <div className="success-content">
              <div className="success-icon">
                <CheckCircleOutlined style={{ fontSize: 64, color: '#52c41a' }} />
              </div>
              <Title level={2} className="success-title">Password Set!</Title>
              <Text className="success-message">
                Your password has been successfully set.
              </Text>
              <Text className="success-subtext">
                Redirecting to login...
              </Text>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="login-container">
      <div className="form-section">
        <div className="form-wrapper">
          <div className="form-brand">
            <Title level={2} className="form-brand-title">smallHR</Title>
          </div>

          <div className="form-content">
            <div className="form-header">
              <Title level={2} className="form-title">Set Up Your Password</Title>
              <Text className="form-subtitle">
                Create a secure password for your account
              </Text>
            </div>

            <Form
              form={form}
              name="setup-password"
              onFinish={onFinish}
              layout="vertical"
              requiredMark={false}
              className="login-form"
              autoComplete="off"
            >
              <Form.Item
                name="newPassword"
                label="New Password"
                rules={[
                  { required: true, message: 'Please enter a password' },
                  { min: 12, message: 'Password must be at least 12 characters' },
                  {
                    pattern: /(?=.*[a-z])/,
                    message: 'Password must contain at least one lowercase letter'
                  },
                  {
                    pattern: /(?=.*[A-Z])/,
                    message: 'Password must contain at least one uppercase letter'
                  },
                  {
                    pattern: /(?=.*\d)/,
                    message: 'Password must contain at least one number'
                  },
                  {
                    pattern: /(?=.*[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?])/,
                    message: 'Password must contain at least one special character'
                  },
                ]}
                help="Password must be at least 12 characters with uppercase, lowercase, number, and special character"
              >
                <Input.Password
                  prefix={<LockOutlined className="input-icon" />}
                  placeholder="Enter your password"
                  className="form-input"
                  autoComplete="new-password"
                />
              </Form.Item>

              <Form.Item
                name="confirmPassword"
                label="Confirm Password"
                dependencies={['newPassword']}
                rules={[
                  { required: true, message: 'Please confirm your password' },
                  ({ getFieldValue }) => ({
                    validator(_, value) {
                      if (!value || getFieldValue('newPassword') === value) {
                        return Promise.resolve();
                      }
                      return Promise.reject(new Error('Passwords do not match'));
                    },
                  }),
                ]}
              >
                <Input.Password
                  prefix={<LockOutlined className="input-icon" />}
                  placeholder="Confirm your password"
                  className="form-input"
                  autoComplete="new-password"
                />
              </Form.Item>

              <Form.Item>
                <Button
                  type="primary"
                  htmlType="submit"
                  loading={loading}
                  className="form-button"
                  icon={<LockOutlined />}
                  block
                >
                  Set Password
                </Button>
              </Form.Item>

              <div style={{ textAlign: 'center', marginTop: 16 }}>
                <Button
                  type="link"
                  icon={<ArrowLeftOutlined />}
                  onClick={() => navigate('/login')}
                  style={{ padding: 0 }}
                >
                  Back to Login
                </Button>
              </div>
            </Form>
          </div>
        </div>
      </div>
    </div>
  );
}

