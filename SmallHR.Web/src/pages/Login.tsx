import { useState } from 'react';
import { Form, Input, Button, Typography } from 'antd';
import { useNavigate, Link } from 'react-router-dom';
import { authAPI } from '../services/api';
import { useAuthStore } from '../store/authStore';
import { useNotification } from '../contexts/NotificationContext';
import { 
  LockOutlined, 
  MailOutlined, 
  LoginOutlined,
  RocketOutlined,
  SafetyOutlined,
  ThunderboltOutlined
} from '@ant-design/icons';
import type { LoginRequest } from '../types/api';
import './Login.css';

const { Title, Text, Paragraph } = Typography;

export default function Login() {
  const navigate = useNavigate();
  const { login } = useAuthStore();
  const notify = useNotification();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);

  const onFinish = async (values: LoginRequest) => {
    setLoading(true);
    try {
      const response = await authAPI.login(values);
      
      // Tokens are stored in httpOnly cookies by backend
      // We only receive user data in the response
      const { user } = response.data;
      
      // Login and fetch permissions (async)
      await login(user);
      
      notify.success('Login Successful', `Welcome back, ${user.firstName}!`);
      navigate('/dashboard');
    } catch (error: any) {
      notify.error(
        'Login Failed',
        error.response?.data?.message || 'Please check your credentials and try again.'
      );
    } finally {
      setLoading(false);
    }
  };

  const demoAccounts = [
    { role: 'SuperAdmin', email: 'superadmin@smallhr.com', password: 'SuperAdmin@123', color: '#0EA5E9' },
    { role: 'Admin', email: 'admin@smallhr.com', password: 'Admin@123', color: '#14B8A6' },
    { role: 'HR Manager', email: 'hr@smallhr.com', password: 'Hr@123', color: '#10B981' },
    { role: 'Employee', email: 'employee@smallhr.com', password: 'Employee@123', color: '#06B6D4' },
  ];

  return (
    <div className="login-container">
      {/* Left Side - Hero Section */}
      <div className="hero-section">
        <div className="hero-content">
          <div className="hero-branding">
            <div className="brand-icon-wrapper">
              <RocketOutlined className="brand-icon-main" />
            </div>
            <Title level={1} className="hero-title">smallHR</Title>
            <Paragraph className="hero-subtitle">
              Manage Your Tiny Team, Effortlessly.
            </Paragraph>
          </div>

          <div className="hero-image-container">
            <img 
              src="/hero-image.png" 
              alt="SmallHR - Manage Your Tiny Team, Effortlessly" 
              className="hero-image"
            />
          </div>

          <div className="hero-footer">
            <div className="hero-features">
              <div className="hero-feature">
                <ThunderboltOutlined />
                <span>Lightning Fast</span>
              </div>
              <div className="hero-feature">
                <SafetyOutlined />
                <span>Secure & Reliable</span>
              </div>
              <div className="hero-feature">
                <RocketOutlined />
                <span>Built for Humans</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Right Side - Login Form */}
      <div className="form-section">
        <div className="form-wrapper">
          <div className="form-brand">
            <Title level={2} className="form-brand-title">smallHR</Title>
          </div>

          <div className="form-content">
            <div className="form-header">
              <Title level={2} className="form-title">Welcome Back!</Title>
              <Text className="form-subtitle">
                Sign in to continue to your dashboard
              </Text>
            </div>

            <Form
              form={form}
              name="login"
              onFinish={onFinish}
              layout="vertical"
              requiredMark={false}
              className="login-form"
              autoComplete="off"
            >
              <Form.Item
                name="email"
                rules={[
                  { required: true, message: 'Email is required' },
                  { type: 'email', message: 'Please enter a valid email' },
                ]}
              >
                <Input 
                  prefix={<MailOutlined className="input-icon" />}
                  placeholder="Email address"
                  className="form-input"
                  autoComplete="email"
                />
              </Form.Item>

              <Form.Item
                name="password"
                rules={[
                  { required: true, message: 'Password is required' },
                  { min: 6, message: 'Password must be at least 6 characters' }
                ]}
              >
                <Input.Password
                  prefix={<LockOutlined className="input-icon" />}
                  placeholder="Password"
                  className="form-input"
                  autoComplete="current-password"
                />
              </Form.Item>

              <Form.Item style={{ marginBottom: 0 }}>
                <Button
                  type="primary"
                  htmlType="submit"
                  loading={loading}
                  block
                  className="submit-button"
                  icon={!loading && <LoginOutlined />}
                >
                  {loading ? 'Signing in...' : 'Login Now'}
                </Button>
              </Form.Item>

              <div style={{ textAlign: 'right', marginTop: 8 }}>
                <Link 
                  to="/forgot-password" 
                  style={{ 
                    color: 'var(--color-text-secondary)', 
                    fontSize: 14,
                    textDecoration: 'none'
                  }}
                >
                  Forgot Password?
                </Link>
              </div>
            </Form>

            {/* Compact Demo Accounts */}
            <div className="demo-section">
              <Text className="demo-section-title">Try Demo Accounts</Text>
              <div className="demo-accounts-compact">
                {demoAccounts.map((account, index) => (
                  <Button 
                    key={index}
                    className="demo-account-btn"
                    onClick={() => {
                      form.setFieldsValue({ 
                        email: account.email, 
                        password: account.password 
                      });
                    }}
                    style={{ borderColor: account.color }}
                  >
                    <span className="demo-btn-role" style={{ color: account.color }}>
                      {account.role}
                    </span>
                  </Button>
                ))}
              </div>
            </div>
          </div>

          <div className="form-footer">
            <Text className="footer-text">© 2025 SmallHR. All rights reserved.</Text>
          </div>
        </div>
      </div>
    </div>
  );
}
