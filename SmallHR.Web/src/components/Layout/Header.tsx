import { useState, useEffect } from 'react';
import { Tooltip, Tag, Breadcrumb } from 'antd';
import {
  FullscreenOutlined,
  FullscreenExitOutlined,
  ClockCircleOutlined,
  ReloadOutlined,
  BulbOutlined,
  BulbFilled,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
} from '@ant-design/icons';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import { useTheme } from '../../contexts/ThemeContext';

interface HeaderProps {
  collapsed: boolean;
  onToggle: () => void;
}

export default function Header({ collapsed, onToggle }: HeaderProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const { user } = useAuthStore();
  const { isDarkMode, toggleDarkMode } = useTheme();
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [currentTime, setCurrentTime] = useState(new Date());
  const [isHovered, setIsHovered] = useState(false);

  // Update time every minute
  useEffect(() => {
    const timer = setInterval(() => setCurrentTime(new Date()), 60000);
    return () => clearInterval(timer);
  }, []);

  const toggleFullscreen = () => {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen();
      setIsFullscreen(true);
    } else {
      document.exitFullscreen();
      setIsFullscreen(false);
    }
  };

  const formatTime = () => {
    return currentTime.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
    });
  };

  const formatDate = () => {
    return currentTime.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
    });
  };

  // Generate breadcrumbs from current location
  const getBreadcrumbs = () => {
    const pathParts = location.pathname.split('/').filter(Boolean);
    
    // If we're on the root or dashboard, just show Dashboard
    if (pathParts.length === 0 || pathParts[0] === 'dashboard') {
      return [{ title: 'Dashboard' }];
    }
    
    // For other pages, start with Home and build the path
    const breadcrumbs: Array<{ title: string; href?: string }> = [{ title: 'Home', href: '/dashboard' }];
    
    pathParts.forEach((part, index) => {
      const isLast = index === pathParts.length - 1;
      const path = '/' + pathParts.slice(0, index + 1).join('/');
      const title = part.charAt(0).toUpperCase() + part.slice(1).replace(/-/g, ' ');
      breadcrumbs.push({
        title,
        href: isLast ? undefined : path,
      });
    });
    
    return breadcrumbs;
  };

  return (
    <div
      style={{
        height: 56,
        background: 'transparent',
        borderBottom: '1px solid var(--color-border)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 16px',
        gap: 16,
      }}
    >
      {/* Left side - Collapse button and Breadcrumbs */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        <div
          onClick={onToggle}
          onMouseEnter={() => setIsHovered(true)}
          onMouseLeave={() => setIsHovered(false)}
          style={{
            width: 32,
            height: 32,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: 'pointer',
            borderRadius: 6,
            transition: 'all 200ms ease-in-out',
            color: 'var(--color-text-secondary)',
            background: isHovered ? 'var(--primary-08a)' : 'transparent',
          }}
        >
          {collapsed ? <MenuUnfoldOutlined style={{ fontSize: 14 }} /> : <MenuFoldOutlined style={{ fontSize: 14 }} />}
        </div>
        
        <Breadcrumb
          items={getBreadcrumbs().map(item => {
            if (item.href) {
              return {
                ...item,
                onClick: () => navigate(item.href!),
              };
            }
            return item;
          })}
          style={{ 
            fontSize: 13,
            fontFamily: 'Inter, sans-serif'
          }}
          separator={<span style={{ color: 'var(--color-text-tertiary)', fontSize: 12 }}>/</span>}
        />
      </div>
      
      {/* Right side actions */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
      {/* Date & Time Display */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 8,
          padding: '6px 12px',
          background: 'var(--glass-background)',
          backdropFilter: 'blur(8px)',
          borderRadius: 8,
          border: '1px solid var(--glass-border)',
        }}
      >
        <ClockCircleOutlined style={{ color: 'var(--color-primary, #4F46E5)', fontSize: 14 }} />
        <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <span style={{ fontSize: '13px', fontWeight: 600, color: 'var(--color-text-primary, #0F172A)', lineHeight: 1, fontFamily: 'Inter, sans-serif' }}>
            {formatTime()}
          </span>
          <span style={{ fontSize: '10px', color: 'var(--color-text-secondary, #64748B)', lineHeight: 1, letterSpacing: '0.02em', fontFamily: 'Inter, sans-serif' }}>
            {formatDate()}
          </span>
        </div>
      </div>

      {/* Dark Mode Toggle */}
      <Tooltip title={isDarkMode ? 'Light Mode' : 'Dark Mode'}>
        <div
          onClick={toggleDarkMode}
          style={{
            width: 36,
            height: 36,
            borderRadius: 8,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: 'pointer',
            transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
            color: 'var(--color-text-secondary, #64748B)',
            background: 'transparent',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.background = 'var(--primary-08a)';
            e.currentTarget.style.color = 'var(--color-primary)';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.background = 'transparent';
            e.currentTarget.style.color = '#64748B';
          }}
        >
          {isDarkMode ? (
            <BulbFilled style={{ fontSize: 16 }} />
          ) : (
            <BulbOutlined style={{ fontSize: 16 }} />
          )}
        </div>
      </Tooltip>

      {/* Refresh Button */}
      <Tooltip title="Refresh Page">
        <div
          onClick={() => window.location.reload()}
          style={{
            width: 36,
            height: 36,
            borderRadius: 8,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: 'pointer',
            transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
            color: 'var(--color-text-secondary, #64748B)',
            background: 'transparent',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.background = 'var(--primary-08a)';
            e.currentTarget.style.color = 'var(--color-primary)';
            e.currentTarget.style.transform = 'rotate(180deg)';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.background = 'transparent';
            e.currentTarget.style.color = 'var(--color-text-secondary, #64748B)';
            e.currentTarget.style.transform = 'rotate(0deg)';
          }}
        >
          <ReloadOutlined style={{ fontSize: 16 }} />
        </div>
      </Tooltip>

      {/* Fullscreen Toggle */}
      <Tooltip title={isFullscreen ? 'Exit Fullscreen' : 'Enter Fullscreen'}>
        <div
          onClick={toggleFullscreen}
          style={{
            width: 36,
            height: 36,
            borderRadius: 8,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: 'pointer',
            transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
            color: 'var(--color-text-secondary, #64748B)',
            background: 'transparent',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.background = 'var(--primary-08a)';
            e.currentTarget.style.color = 'var(--color-primary)';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.background = 'transparent';
            e.currentTarget.style.color = '#64748B';
          }}
        >
          {isFullscreen ? (
            <FullscreenExitOutlined style={{ fontSize: 16 }} />
          ) : (
            <FullscreenOutlined style={{ fontSize: 16 }} />
          )}
        </div>
      </Tooltip>

      {/* User Role Badge */}
      <Tag
        style={{
          borderRadius: 'var(--radius-full)',
          padding: '6px 12px',
          fontSize: 11,
          fontWeight: 600,
          border: 'none',
          background: 'var(--gradient-primary)',
          color: 'white',
          boxShadow: '0 2px 8px rgba(79, 70, 229, 0.3)',
          margin: 0,
        }}
      >
        {user?.roles?.[0] || 'User'}
      </Tag>
      </div>
    </div>
  );
}

