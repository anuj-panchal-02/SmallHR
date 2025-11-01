import { ReactNode, useState } from 'react';
import Sidebar from './Sidebar';
import Header from './Header';

interface MainLayoutProps {
  children: ReactNode;
}

export default function MainLayout({ children }: MainLayoutProps) {
  const [collapsed, setCollapsed] = useState(false);

  const handleToggle = () => {
    setCollapsed(!collapsed);
  };

  return (
    <div style={{ display: 'flex', minHeight: '100vh', background: 'var(--color-background)' }}>
      <Sidebar collapsed={collapsed} />

      <div
        style={{
          marginLeft: collapsed ? 80 : 280,
          flex: 1,
          display: 'flex',
          flexDirection: 'column',
          transition: 'margin-left 250ms ease-in-out',
          background: 'var(--color-sidebar)',
          position: 'relative',
          zIndex: 1000,
          minHeight: '100vh',
          padding: '16px',
        }}
      >
        {/* Main Content Container with Header Inside */}
        <div
          style={{
            display: 'flex',
            flexDirection: 'column',
            flex: 1,
            background: 'var(--color-surface)',
            borderRadius: 16,
            boxShadow: 'var(--card-shadow)',
            overflow: 'hidden',
          }}
        >
          {/* Header - Inside main content card */}
          <Header collapsed={collapsed} onToggle={handleToggle} />
          
          {/* Main Content */}
          <main
            style={{
              flex: 1,
              overflowY: 'auto',
              padding: '24px',
              background: 'transparent',
            }}
          >
            {children}
          </main>
        </div>
      </div>
    </div>
  );
}

