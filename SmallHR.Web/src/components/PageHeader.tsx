import { useState, useEffect } from 'react';
import { Button } from 'antd';
import { ClockCircleOutlined } from '@ant-design/icons';
import { useAuthStore } from '../store/authStore';

interface PageHeaderProps {
  title: string;
  subtitle?: string;
}

export default function PageHeader({ title, subtitle }: PageHeaderProps) {
  const { user } = useAuthStore();
  const [currentTime, setCurrentTime] = useState(new Date());

  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentTime(new Date());
    }, 1000);

    return () => clearInterval(timer);
  }, []);

  const formatTime = (date: Date) => {
    return date.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
    });
  };

  const formatDate = (date: Date) => {
    return date.toLocaleDateString('en-US', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  };

  return (
    <div
      style={{
        padding: '12px 16px',
        marginBottom: 12,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        minHeight: 48,
      }}
    >
      {/* Left Side - Title, Subtitle and Role in single line */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        <h2
          style={{
            margin: 0,
            fontSize: 16,
            fontWeight: 600,
            color: 'var(--color-text-primary, #1E293B)',
            fontFamily: 'Inter, sans-serif',
            letterSpacing: '-0.02em',
          }}
        >
          {title}
        </h2>
        {subtitle && (
          <>
            <span style={{ color: 'var(--color-text-tertiary, #CBD5E1)', fontSize: 20, lineHeight: 1, fontFamily: 'Inter, sans-serif' }}>•</span>
            <span
              style={{
            fontSize: 13,
            color: 'var(--color-text-secondary, #64748B)',
            fontFamily: 'Inter, sans-serif',
            fontWeight: 500,
          }}
        >
          {subtitle}
        </span>
          </>
        )}
        <span
          style={{
            padding: 'var(--badge-padding)',
            background: 'var(--gradient-accent)',
            borderRadius: 'var(--badge-radius)',
            fontSize: 'var(--badge-font-size)',
            fontWeight: 'var(--badge-font-weight)',
            color: 'var(--color-white)',
            boxShadow: 'var(--badge-shadow)',
            textTransform: 'uppercase',
            letterSpacing: 'var(--badge-letter-spacing)',
            fontFamily: 'var(--button-font-family)',
          }}
        >
          {user?.roles?.[0] || 'User'}
        </span>
      </div>

      {/* Right Side - Time, Date and Clock In in single line */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        {/* Time and Date in single line */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <div
            style={{
              fontSize: 16,
              fontWeight: 600,
              color: 'var(--color-text-primary, #1E293B)',
              fontFamily: 'Inter, sans-serif',
              letterSpacing: '-0.02em',
            }}
          >
            {formatTime(currentTime)}
          </div>
          <span style={{ color: 'var(--color-text-tertiary, #CBD5E1)', fontSize: 16, lineHeight: 1, fontFamily: 'Inter, sans-serif' }}>•</span>
          <div
            style={{
              fontSize: 12,
              color: 'var(--color-text-secondary, #64748B)',
              fontWeight: 500,
              fontFamily: 'Inter, sans-serif',
            }}
          >
            {formatDate(currentTime)}
          </div>
        </div>

        {/* Clock In Button */}
        <Button
          type="primary"
          icon={<ClockCircleOutlined />}
          size="small"
          style={{
            height: 32,
            paddingLeft: 12,
            paddingRight: 12,
            borderRadius: 8,
            fontWeight: 600,
            background: 'var(--gradient-success)',
            border: 'none',
            boxShadow: '0 2px 8px rgba(16, 185, 129, 0.2)',
            fontSize: 13,
            fontFamily: 'Inter, sans-serif',
            transition: 'all 250ms ease-in-out',
            letterSpacing: '-0.01em',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.transform = 'translateY(-1px)';
            e.currentTarget.style.boxShadow = '0 4px 12px rgba(16, 185, 129, 0.3)';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.transform = 'translateY(0)';
            e.currentTarget.style.boxShadow = '0 2px 8px rgba(16, 185, 129, 0.2)';
          }}
        >
          Clock In
        </Button>
      </div>
    </div>
  );
}

