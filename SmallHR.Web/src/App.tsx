import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ConfigProvider, App as AntApp } from 'antd';
import { useAuthStore } from './store/authStore';
import { NotificationProvider } from './contexts/NotificationContext';
import { ThemeProvider } from './contexts/ThemeContext';
import MainLayout from './components/Layout/MainLayout';
import Login from './pages/Login';
import ForgotPassword from './pages/ForgotPassword';
import ResetPassword from './pages/ResetPassword';
import SetupPassword from './pages/SetupPassword';
import VerifyEmail from './pages/VerifyEmail';
import SuperAdminDashboard from './pages/SuperAdminDashboard';
import AdminDashboard from './pages/AdminDashboard';
import HRDashboard from './pages/HRDashboard';
import EmployeeDashboard from './pages/EmployeeDashboard';
import RolePermissions from './pages/RolePermissions';
import Employees from './pages/Employees';
import Departments from './pages/Departments';
import Positions from './pages/Positions';
import TenantSettings from './pages/TenantSettings';
import TenantsList from './pages/TenantsList';
import TenantDetail from './pages/TenantDetail';
import BillingCenter from './pages/BillingCenter';
import AlertsHub from './pages/AlertsHub';
import UsageDashboard from './pages/UsageDashboard';
import { ProtectedRoute } from './components/ProtectedRoute';
import './App.css';
import { buildSemanticColors, buildSemanticColorsDark, applyCssVariables, getAntThemeFromSemantic, getStoredPaletteOrDefault, registerGlobalPaletteAPI } from './theme';
import { useTheme } from './contexts/ThemeContext';
import { useModulesStore } from './store/modulesStore';
import UnknownModule from './components/UnknownModule';
import type { ModuleNode } from './services/modules';
import { useEffect, useState } from 'react';

// Auth Route Component
function AuthRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuthStore();
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" />;
}

// Role-based Dashboard Component
function DashboardRouter() {
  const { user } = useAuthStore();
  const userRole = user?.roles?.[0]?.toLowerCase();

  if (userRole === 'superadmin') {
    return <SuperAdminDashboard />;
  } else if (userRole === 'admin') {
    return <AdminDashboard />;
  } else if (userRole === 'hr') {
    return <HRDashboard />;
  } else {
    return <EmployeeDashboard />;
  }
}

function AppShell() {
  const { modules, refresh } = useModulesStore();
  const { isAuthenticated } = useAuthStore();
  const { isDarkMode } = useTheme();
  const [palette, setPalette] = useState(getStoredPaletteOrDefault());

  useEffect(() => {
    refresh();
  }, [refresh]);

  // Init palette API and listen for quick switches
  useEffect(() => {
    registerGlobalPaletteAPI();
    const handler = (e: any) => { setPalette(e.detail || getStoredPaletteOrDefault()); };
    window.addEventListener('palettechange', handler);
    return () => window.removeEventListener('palettechange', handler);
  }, []);

  // Re-apply CSS variables when theme mode or palette changes
  useEffect(() => {
    const sem = isDarkMode ? buildSemanticColorsDark(palette) : buildSemanticColors(palette);
    applyCssVariables(sem);
  }, [isDarkMode, palette]);

  // Map known module paths to existing pages/components
  const routeForPath = (path: string) => {
    if (path === '/dashboard') return <DashboardRouter />;
    if (path === '/role-permissions') return <RolePermissions />;
    if (path === '/employees') return <Employees />;
    if (path === '/departments') return <Departments />;
    if (path === '/positions') return <Positions />;
    if (path === '/organization') {
      // Organization is a parent, default to Departments
      return <Departments />;
    }
    if (path === '/tenant-settings') return <TenantSettings />;
    if (path === '/admin/tenants') return <TenantsList />;
    if (path === '/admin/tenants/:id') return <TenantDetail />;
    if (path === '/admin/billing') return <BillingCenter />;
    if (path === '/admin/alerts') return <AlertsHub />;
    if (path === '/admin/usage') return <UsageDashboard />;
    // Payroll pages
    if (path === '/payroll/reports') return <UnknownModule />;
    if (path === '/payroll/settings') return <UnknownModule />;
    if (path === '/payroll') return <UnknownModule />;
    // Other pages
    if (path === '/calendar') return <UnknownModule />;
    if (path === '/notice-board') return <UnknownModule />;
    if (path === '/expenses') return <UnknownModule />;
    if (path === '/settings') return <UnknownModule />;
    return <UnknownModule />;
  };

  const renderModuleRoutes = (nodes: ModuleNode[]): JSX.Element[] =>
    nodes.flatMap((n): JSX.Element[] => {
      // Skip dashboard since we have a static route for it
      if (n.path === '/dashboard') {
        return n.children ? renderModuleRoutes(n.children) : [];
      }
      return [
        <Route
          key={n.path}
          path={n.path}
          element={
            <AuthRoute>
              <ProtectedRoute requiredPath={n.path}>
                <MainLayout>
                  {routeForPath(n.path)}
                </MainLayout>
              </ProtectedRoute>
            </AuthRoute>
          }
        />,
        ...(n.children ? renderModuleRoutes(n.children) : []),
      ];
    });

  return (
      <ConfigProvider theme={getAntThemeFromSemantic(isDarkMode ? buildSemanticColorsDark(palette) : buildSemanticColors(palette))}>
      <AntApp>
        <NotificationProvider>
          <BrowserRouter>
            <Routes>
              <Route path="/login" element={<Login />} />
              <Route path="/forgot-password" element={<ForgotPassword />} />
              <Route path="/reset-password" element={<ResetPassword />} />
              <Route path="/setup-password" element={<SetupPassword />} />
              <Route path="/verify-email" element={<VerifyEmail />} />
              {/* Static dashboard route - always available */}
              <Route
                path="/admin/usage"
                element={
                  <AuthRoute>
                    <MainLayout>
                      <UsageDashboard />
                    </MainLayout>
                  </AuthRoute>
                }
              />
              <Route
                path="/dashboard"
                element={
                  <AuthRoute>
                    <ProtectedRoute requiredPath="/dashboard">
                      <MainLayout>
                        <DashboardRouter />
                      </MainLayout>
                    </ProtectedRoute>
                  </AuthRoute>
                }
              />
              {/* Admin routes - SuperAdmin only */}
              <Route
                path="/admin/dashboard"
                element={
                  <AuthRoute>
                    <MainLayout>
                      <SuperAdminDashboard />
                    </MainLayout>
                  </AuthRoute>
                }
              />
              <Route
                path="/admin/tenants"
                element={
                  <AuthRoute>
                    <MainLayout>
                      <TenantsList />
                    </MainLayout>
                  </AuthRoute>
                }
              />
              <Route
                path="/admin/tenants/:id"
                element={
                  <AuthRoute>
                    <MainLayout>
                      <TenantDetail />
                    </MainLayout>
                  </AuthRoute>
                }
              />
              <Route
                path="/admin/billing"
                element={
                  <AuthRoute>
                    <MainLayout>
                      <BillingCenter />
                    </MainLayout>
                  </AuthRoute>
                }
              />
              <Route
                path="/admin/alerts"
                element={
                  <AuthRoute>
                    <MainLayout>
                      <AlertsHub />
                    </MainLayout>
                  </AuthRoute>
                }
              />
              {/* Static route for Role Permissions */}
              <Route
                path="/role-permissions"
                element={
                  <AuthRoute>
                    <ProtectedRoute requiredPath="/role-permissions">
                      <MainLayout>
                        <RolePermissions />
                      </MainLayout>
                    </ProtectedRoute>
                  </AuthRoute>
                }
              />
              {/* Dynamic module routes */}
              {renderModuleRoutes(modules)}
              <Route
                path="/"
                element={
                  isAuthenticated ? <Navigate to="/dashboard" /> : <Navigate to="/login" />
                }
              />
            </Routes>
          </BrowserRouter>
        </NotificationProvider>
      </AntApp>
    </ConfigProvider>
  );
}

function App() {
  return (
    <ThemeProvider>
      <AppShell />
    </ThemeProvider>
  );
}

export default App;

