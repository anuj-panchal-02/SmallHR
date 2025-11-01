import { useState } from 'react';
import { Form, Input, Button, Typography, message } from 'antd';
import { useNavigate, Link } from 'react-router-dom';
import { authAPI } from '../services/api';
import { 
  MailOutlined, 
  ArrowLeftOutlined,
  CheckCircleOutlined
} from '@ant-design/icons';
import './Login.css';

const { Title, Text } = Typography;

export default function ForgotPassword() {
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);

  const onFinish = async (values: { email: string }) => {
    setLoading(true);
    try {
      await authAPI.forgotPassword(values.email);
      setSuccess(true);
      message.success('Reset instructions sent to your email!');
    } catch (error: any) {
      console.error('Password reset request failed:', error);
      message.error(error.response?.data?.message || 'Failed to send reset instructions');
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
              <Title level={2} className="success-title">Check Your Email</Title>
              <Text className="success-message">
                We've sent password reset instructions to your email address.
              </Text>
              <Text className="success-subtext">
                Didn't receive the email? Check your spam folder or try again.
              </Text>
              <div style={{ marginTop: 24 }}>
                <Button
                  type="link"
                  icon={<ArrowLeftOutlined />}
                  onClick={() => navigate('/login')}
                  style={{ padding: 0 }}
                >
                  Back to Login
                </Button>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="login-container">
      {/* Right Side - Forgot Password Form */}
      <div className="form-section">
        <div className="form-wrapper">
          <div className="form-brand">
            <Title level={2} className="form-brand-title">smallHR</Title>
          </div>

          <div className="form-content">
            <div className="form-header">
              <Title level={2} className="form-title">Forgot Password?</Title>
              <Text className="form-subtitle">
                Enter your email address and we'll send you reset instructions
              </Text>
            </div>

            <Form
              form={form}
              name="forgot-password"
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

              <Form.Item>
                <Button
                  type="primary"
                  htmlType="submit"
                  loading={loading}
                  className="form-button"
                  icon={<MailOutlined />}
                >
                  Send Reset Instructions
                </Button>
              </Form.Item>

              <div style={{ textAlign: 'center', marginTop: 16 }}>
                <Link to="/login" style={{ color: 'var(--color-text-secondary)' }}>
                  <ArrowLeftOutlined /> Back to Login
                </Link>
              </div>
            </Form>
          </div>
        </div>
      </div>
    </div>
  );
}

