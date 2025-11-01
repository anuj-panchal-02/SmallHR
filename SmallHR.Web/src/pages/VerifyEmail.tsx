import { useState, useEffect } from 'react';
import { Button, Typography, message, Spin } from 'antd';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { authAPI } from '../services/api';
import { 
  CheckCircleOutlined,
  CloseCircleOutlined,
  MailOutlined,
  ArrowLeftOutlined
} from '@ant-design/icons';
import './Login.css';

const { Title, Text } = Typography;

export default function VerifyEmail() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const [resendLoading, setResendLoading] = useState(false);
  const [email, setEmail] = useState<string | null>(null);

  useEffect(() => {
    const token = searchParams.get('token');
    const userId = searchParams.get('userId');
    const emailParam = searchParams.get('email');
    
    if (!token || !userId) {
      setStatus('error');
      message.error('Invalid verification link');
      return;
    }
    
    if (emailParam) {
      setEmail(emailParam);
    }
    
    verifyEmail(token, userId);
  }, [searchParams]);

  const verifyEmail = async (token: string, userId: string) => {
    try {
      await authAPI.verifyEmail(token, userId);
      setStatus('success');
      message.success('Email verified successfully!');
      
      // Redirect to login after 3 seconds
      setTimeout(() => {
        navigate('/login');
      }, 3000);
    } catch (error: any) {
      console.error('Email verification failed:', error);
      setStatus('error');
      message.error(error.response?.data?.message || 'Email verification failed');
    }
  };

  const handleResend = async () => {
    if (!email) {
      message.error('Email not found');
      return;
    }

    setResendLoading(true);
    try {
      await authAPI.resendVerification(email);
      message.success('Verification email sent!');
    } catch (error: any) {
      console.error('Resend failed:', error);
      message.error(error.response?.data?.message || 'Failed to resend verification email');
    } finally {
      setResendLoading(false);
    }
  };

  if (status === 'loading') {
    return (
      <div className="login-container">
        <div className="form-section">
          <div className="form-wrapper">
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: 400 }}>
              <Spin size="large" />
              <Text style={{ marginTop: 16, color: 'var(--color-text-secondary)' }}>
                Verifying your email...
              </Text>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (status === 'success') {
    return (
      <div className="login-container">
        <div className="form-section">
          <div className="form-wrapper">
            <div className="success-content">
              <div className="success-icon">
                <CheckCircleOutlined style={{ fontSize: 64, color: '#52c41a' }} />
              </div>
              <Title level={2} className="success-title">Email Verified!</Title>
              <Text className="success-message">
                Your email has been successfully verified.
              </Text>
              <Text className="success-subtext">
                Redirecting to login...
              </Text>
              <div style={{ marginTop: 24 }}>
                <Link to="/login">
                  <Button type="primary" icon={<ArrowLeftOutlined />}>
                    Go to Login
                  </Button>
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Error state
  return (
    <div className="login-container">
      <div className="form-section">
        <div className="form-wrapper">
          <div className="success-content">
            <div className="success-icon">
              <CloseCircleOutlined style={{ fontSize: 64, color: '#ff4d4f' }} />
            </div>
            <Title level={2} className="success-title">Verification Failed</Title>
            <Text className="success-message">
              The verification link is invalid or has expired.
            </Text>
            {email && (
              <>
                <Text className="success-subtext">
                  Click below to receive a new verification link.
                </Text>
                <div style={{ marginTop: 24 }}>
                  <Button
                    type="primary"
                    icon={<MailOutlined />}
                    loading={resendLoading}
                    onClick={handleResend}
                  >
                    Resend Verification Email
                  </Button>
                </div>
              </>
            )}
            <div style={{ marginTop: 16 }}>
              <Link to="/login" style={{ color: 'var(--color-text-secondary)' }}>
                <ArrowLeftOutlined /> Back to Login
              </Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

