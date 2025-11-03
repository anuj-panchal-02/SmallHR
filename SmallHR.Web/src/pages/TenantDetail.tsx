import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Card, Row, Col, Statistic, Tag, Button, Table, Tabs, Timeline, Space, Descriptions, Modal, Input } from 'antd';
import {
  ArrowLeftOutlined,
  UserSwitchOutlined,
  UserOutlined,
  TeamOutlined,
  ApiOutlined,
  KeyOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import PageHeader from '../components/PageHeader';
import api from '../services/api';
import { useNotification } from '../contexts/NotificationContext';
// Charts will be added later - using recharts if needed

interface TenantDetail {
  id: number;
  name: string;
  domain: string | null;
  status: string;
  isActive: boolean;
  isSubscriptionActive: boolean;
  adminEmail: string | null;
  adminFirstName: string | null;
  adminLastName: string | null;
  createdAt: string;
  updatedAt: string;
  userCount: number;
  employeeCount: number;
  usageMetrics: {
    apiRequestCount: number;
    employeeCount: number;
    userCount: number;
    lastUpdated: string;
  } | null;
  subscription: {
    id: number;
    planName: string;
    status: string;
    price: number;
    billingPeriod: string;
    startDate: string;
    endDate: string | null;
    createdAt: string;
  } | null;
  subscriptions?: Array<{
    id: number;
    planName: string;
    status: string;
    price: number;
    billingPeriod: string;
    startDate: string;
    endDate: string | null;
    createdAt: string;
  }>;
  recentLifecycleEvents?: Array<{
    eventType: string;
    eventDate: string;
    description: string | null;
    metadata: string | null;
  }>;
}

interface LogEntry {
  id: number;
  actionType: string;
  httpMethod: string;
  endpoint: string;
  statusCode: number;
  isSuccess: boolean;
  createdAt: string;
  adminEmail: string;
}

export default function TenantDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const notify = useNotification();
  const [tenant, setTenant] = useState<TenantDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [logsLoading, setLogsLoading] = useState(false);
  const [activeTab, setActiveTab] = useState('overview');

  useEffect(() => {
    if (id) {
      fetchTenantDetail();
      if (activeTab === 'logs') {
        fetchLogs();
      }
    }
  }, [id, activeTab]);

  const fetchTenantDetail = async () => {
    setLoading(true);
    try {
      const response = await api.get(`/admin/tenants/${id}`);
      console.log('Tenant detail API Response:', response);
      console.log('Tenant detail Response Data:', response.data);
      console.log('Response Data Keys:', response.data ? Object.keys(response.data) : 'null');
      
      // Handle both camelCase and PascalCase property names
      // Strongly-typed DTO will use PascalCase, but handle both for compatibility
      const tenantData: TenantDetail = {
        ...response.data,
        id: response.data.id || response.data.Id || 0,
        name: response.data.name || response.data.Name || '',
        domain: response.data.domain ?? response.data.Domain ?? null,
        status: response.data.status || response.data.Status || 'Unknown',
        isActive: response.data.isActive ?? response.data.IsActive ?? false,
        isSubscriptionActive: response.data.isSubscriptionActive ?? response.data.IsSubscriptionActive ?? false,
        adminEmail: response.data.adminEmail ?? response.data.AdminEmail ?? null,
        adminFirstName: response.data.adminFirstName ?? response.data.AdminFirstName ?? null,
        adminLastName: response.data.adminLastName ?? response.data.AdminLastName ?? null,
        createdAt: response.data.createdAt || response.data.CreatedAt || new Date().toISOString(),
        updatedAt: response.data.updatedAt || response.data.UpdatedAt || response.data.createdAt || response.data.CreatedAt || new Date().toISOString(),
        userCount: response.data.userCount ?? response.data.UserCount ?? 0,
        employeeCount: response.data.employeeCount ?? response.data.EmployeeCount ?? 0,
        usageMetrics: response.data.usageMetrics || response.data.UsageMetrics || null,
        subscription: response.data.subscription || response.data.Subscription || null,
        recentLifecycleEvents: response.data.recentLifecycleEvents || response.data.RecentLifecycleEvents || [],
        subscriptions: response.data.subscriptions || response.data.Subscriptions || [],
      };
      
      console.log('Processed tenant data:', tenantData);
      console.log('Tenant name:', tenantData.name);
      console.log('Subscriptions count:', tenantData.subscriptions?.length || 0);
      console.log('Lifecycle events count:', tenantData.recentLifecycleEvents?.length || 0);
      
      // Ensure we have valid data
      if (!tenantData.name) {
        console.error('Tenant data is missing name:', tenantData);
        notify.error('Failed to Load Tenant', 'Invalid tenant data received from server');
        return;
      }
      
      setTenant(tenantData);
    } catch (error: any) {
      console.error('Error fetching tenant detail:', error);
      console.error('Error response:', error.response?.data);
      notify.error('Failed to Load Tenant', error.response?.data?.message || 'Unable to fetch tenant details.');
    } finally {
      setLoading(false);
    }
  };

  const fetchLogs = async () => {
    setLogsLoading(true);
    try {
      const response = await api.get('/adminaudit', {
        params: {
          targetTenantId: id,
          pageSize: 100,
          pageNumber: 1,
        },
      });
      setLogs(response.data.auditLogs || []);
    } catch (error: any) {
      notify.error('Failed to Load Logs', 'Unable to fetch audit logs.');
      console.error(error);
    } finally {
      setLogsLoading(false);
    }
  };

  const handleImpersonate = async () => {
    if (!tenant) return;

    try {
      const response = await api.post(`/admin/tenants/${tenant.id}/impersonate?durationMinutes=30`);
      
      console.log('Impersonate response:', response);
      console.log('Impersonate response.data:', response.data);
      console.log('Impersonate response.data keys:', response.data ? Object.keys(response.data) : 'null');
      
      // Handle different response structures (result, value, or direct properties)
      // Strongly-typed DTO will use PascalCase, but handle both for compatibility
      const responseData = response.data.result || response.data.value || response.data;
      console.log('Processed responseData:', responseData);
      
      // Handle both camelCase and PascalCase property names (DTO uses PascalCase)
      const tenantData = responseData.tenant || responseData.Tenant || response.data.tenant || response.data.Tenant;
      const impersonationToken = responseData.impersonationToken || responseData.ImpersonationToken || response.data.impersonationToken || response.data.ImpersonationToken;
      const expiresAt = responseData.expiresAt || responseData.ExpiresAt || response.data.expiresAt || response.data.ExpiresAt;
      const banner = responseData.banner || responseData.Banner || response.data.banner || response.data.Banner || responseData.message || responseData.Message;
      
      console.log('Extracted values:', { tenantData, impersonationToken, expiresAt, banner });
      console.log('Tenant data structure:', tenantData);
      console.log('Tenant data keys:', tenantData ? Object.keys(tenantData) : 'null');
      
      if (!tenantData) {
        console.error('Tenant data not found in response. Full response:', JSON.stringify(response.data, null, 2));
        notify.error('Impersonation Failed', 'Invalid response from server - tenant data missing');
        return;
      }
      
      if (!impersonationToken) {
        console.error('Impersonation token not found in response. Full response:', JSON.stringify(response.data, null, 2));
        notify.error('Impersonation Failed', 'Invalid response from server - impersonation token missing');
        return;
      }
      
      // Store impersonation token
      localStorage.setItem('impersonationToken', impersonationToken);
      localStorage.setItem('impersonatedTenant', JSON.stringify({
        id: tenantData.id || tenantData.Id,
        name: tenantData.name || tenantData.Name,
        expiresAt: expiresAt,
      }));

      // Update API client
      api.defaults.headers.common['Authorization'] = `Bearer ${impersonationToken}`;

      notify.success('Impersonation Started', banner || 'Impersonation started successfully');
      
      // Redirect to tenant dashboard or refresh
      window.location.href = '/dashboard';
    } catch (error: any) {
      console.error('Impersonation error:', error);
      console.error('Error response:', error.response?.data);
      notify.error('Impersonation Failed', error.response?.data?.message || 'Unable to impersonate tenant.');
    }
  };

  const handleSuspend = async () => {
    if (!tenant || !tenant.id) {
      console.error('Cannot suspend: tenant is null or missing id');
      return;
    }

    try {
      await api.post(`/admin/tenants/${tenant.id}/suspend`);
      notify.success('Tenant Suspended', `${tenant.name || 'Tenant'} has been suspended.`);
      fetchTenantDetail();
    } catch (error: any) {
      notify.error('Failed to Suspend', error.response?.data?.message || 'Unable to suspend tenant.');
      console.error(error);
    }
  };

  const handleResume = async () => {
    if (!tenant || !tenant.id) {
      console.error('Cannot resume: tenant is null or missing id');
      return;
    }

    try {
      await api.post(`/admin/tenants/${tenant.id}/resume`);
      notify.success('Tenant Resumed', `${tenant.name || 'Tenant'} has been resumed.`);
      fetchTenantDetail();
    } catch (error: any) {
      notify.error('Failed to Resume', error.response?.data?.message || 'Unable to resume tenant.');
      console.error(error);
    }
  };

  const handleGetAdminSetupLink = async () => {
    if (!tenant || !tenant.id) {
      console.error('Cannot get admin setup link: tenant is null or missing id');
      return;
    }

    try {
      const response = await api.get(`/admin/tenants/${tenant.id}/admin-setup-link`);
      const setupLink = response.data?.setupLink;
      const adminEmail = response.data?.adminEmail;

      if (setupLink) {
        Modal.info({
          title: 'Admin Password Setup Link',
          width: 700,
          content: (
            <div>
              <p><strong>Tenant:</strong> {tenant.name}</p>
              <p><strong>Admin Email:</strong> {adminEmail}</p>
              <p style={{ marginTop: 16, marginBottom: 8 }}><strong>Password Setup Link:</strong></p>
              <Input.TextArea
                value={setupLink}
                readOnly
                autoSize={{ minRows: 2, maxRows: 4 }}
                style={{ fontFamily: 'monospace', fontSize: 12 }}
              />
              <p style={{ marginTop: 16, color: '#666', fontSize: 12 }}>
                Copy this link and share it with the admin user. They can use it to set their password.
                <br />
                <strong>Note:</strong> This link expires in 7 days.
              </p>
            </div>
          ),
          okText: 'Copy Link',
          onOk: () => {
            navigator.clipboard.writeText(setupLink);
            notify.success('Link Copied', 'Password setup link has been copied to clipboard.');
          }
        });
      }
    } catch (error: any) {
      const status = error.response?.status;
      const errorData = error.response?.data;
      
      if (status === 503) {
        // Provisioning in progress - show retry option
        Modal.confirm({
          title: 'Provisioning in Progress',
          content: (
            <div>
              <p>{errorData?.message || 'The tenant is currently being provisioned. The admin user is not yet created.'}</p>
              <p style={{ marginTop: 12, color: '#666', fontSize: 12 }}>
                <strong>Suggestions:</strong>
                <br />• Wait a few seconds and try again
                <br />• The backend will automatically retry up to 10 times (10 seconds max)
                <br />• Or trigger provisioning manually via the API
              </p>
            </div>
          ),
          okText: 'Retry Now',
          cancelText: 'Cancel',
          onOk: () => {
            // Retry after a short delay
            setTimeout(() => {
              handleGetAdminSetupLink();
            }, 2000);
          }
        });
      } else if (status === 404) {
        // Admin user not found - show suggestion
        notify.warning(
          'Admin User Not Found',
          errorData?.message || 'The admin user has not been created yet. Try triggering provisioning first.'
        );
      } else {
        notify.error(
          'Failed to Get Setup Link',
          errorData?.message || 'Unable to generate setup link. The tenant may not be fully provisioned yet.'
        );
      }
      console.error(error);
    }
  };

  const getStatusTag = (status: string) => {
    const statusConfig: Record<string, { color: string }> = {
      Active: { color: 'green' },
      Suspended: { color: 'red' },
      Provisioning: { color: 'blue' },
      Cancelled: { color: 'orange' },
      Deleted: { color: 'default' },
    };
    const config = statusConfig[status] || { color: 'default' };
    return <Tag color={config.color}>{status}</Tag>;
  };

  type SubscriptionItem = {
    id: number;
    planName: string;
    status: string;
    price: number;
    billingPeriod: string;
    startDate: string;
    endDate: string | null;
    createdAt: string;
  };
  
  const subscriptionColumns: ColumnsType<SubscriptionItem> = [
    {
      title: 'Plan',
      dataIndex: 'planName',
      key: 'planName',
      render: (text: string) => <Tag color="blue">{text}</Tag>,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => <Tag color={status === 'Active' ? 'green' : 'orange'}>{status}</Tag>,
    },
    {
      title: 'Price',
      dataIndex: 'price',
      key: 'price',
      render: (price: number, record: any) => `$${price.toFixed(2)} / ${record.billingPeriod}`,
    },
    {
      title: 'Start Date',
      dataIndex: 'startDate',
      key: 'startDate',
      render: (date: string) => new Date(date).toLocaleDateString(),
    },
    {
      title: 'End Date',
      dataIndex: 'endDate',
      key: 'endDate',
      render: (date: string | null) => date ? new Date(date).toLocaleDateString() : <Tag>Ongoing</Tag>,
    },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => new Date(date).toLocaleDateString(),
    },
  ];

  const logColumns: ColumnsType<LogEntry> = [
    {
      title: 'Date',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => new Date(date).toLocaleString(),
    },
    {
      title: 'Action',
      dataIndex: 'actionType',
      key: 'actionType',
    },
    {
      title: 'Method',
      dataIndex: 'httpMethod',
      key: 'httpMethod',
      width: 80,
    },
    {
      title: 'Endpoint',
      dataIndex: 'endpoint',
      key: 'endpoint',
      ellipsis: true,
    },
    {
      title: 'Status',
      key: 'status',
      render: (_: any, record: LogEntry) => (
        <Tag color={record.isSuccess ? 'green' : 'red'}>
          {record.statusCode} {record.isSuccess ? 'Success' : 'Failed'}
        </Tag>
      ),
    },
    {
      title: 'Admin',
      dataIndex: 'adminEmail',
      key: 'adminEmail',
    },
  ];

  // Usage chart data (mock for now - replace with real data)
  const usageChartData = tenant?.usageMetrics ? [
    { date: '2024-01', apiRequests: tenant.usageMetrics.apiRequestCount || 0 },
    { date: '2024-02', apiRequests: tenant.usageMetrics.apiRequestCount || 0 },
    { date: '2024-03', apiRequests: tenant.usageMetrics.apiRequestCount || 0 },
  ] : [];

  if (!tenant && !loading) {
    return <div>Tenant not found</div>;
  }

  return (
    <div>
      <Space style={{ marginBottom: 16, alignItems: 'center' }}>
        <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/admin/tenants')}>
          Back
        </Button>
        <PageHeader title={tenant?.name || 'Tenant Details'} />
      </Space>

      <Card loading={loading}>
        <Row gutter={16} style={{ marginBottom: 16 }}>
          <Col span={6}>
            <Statistic
              title="Users"
              value={tenant?.userCount || 0}
              prefix={<UserOutlined />}
            />
          </Col>
          <Col span={6}>
            <Statistic
              title="Employees"
              value={tenant?.employeeCount || 0}
              prefix={<TeamOutlined />}
            />
          </Col>
          <Col span={6}>
            <Statistic
              title="API Requests"
              value={tenant?.usageMetrics?.apiRequestCount || 0}
              prefix={<ApiOutlined />}
            />
          </Col>
          <Col span={6}>
            <Statistic
              title="Status"
              value={tenant?.status || 'Unknown'}
              valueStyle={{ color: tenant?.status === 'Active' ? '#3f8600' : '#cf1322' }}
            />
          </Col>
        </Row>

        <Descriptions bordered column={2} style={{ marginBottom: 16 }}>
          <Descriptions.Item label="Name">{tenant?.name}</Descriptions.Item>
          <Descriptions.Item label="Domain">{tenant?.domain || 'Not Set'}</Descriptions.Item>
          <Descriptions.Item label="Status">{tenant ? getStatusTag(tenant.status) : null}</Descriptions.Item>
          <Descriptions.Item label="Subscription Active">
            {tenant?.isSubscriptionActive ? <Tag color="green">Active</Tag> : <Tag color="red">Inactive</Tag>}
          </Descriptions.Item>
          <Descriptions.Item label="Admin Email">{tenant?.adminEmail || 'Not Set'}</Descriptions.Item>
          <Descriptions.Item label="Admin Name">
            {tenant?.adminFirstName && tenant?.adminLastName
              ? `${tenant.adminFirstName} ${tenant.adminLastName}`
              : 'Not Set'}
          </Descriptions.Item>
          <Descriptions.Item label="Created">
            {tenant ? new Date(tenant.createdAt).toLocaleString() : ''}
          </Descriptions.Item>
          <Descriptions.Item label="Last Updated">
            {tenant ? new Date(tenant.updatedAt).toLocaleString() : ''}
          </Descriptions.Item>
        </Descriptions>

        <Space style={{ marginBottom: 16 }}>
          <Button
            type="primary"
            icon={<UserSwitchOutlined />}
            onClick={handleImpersonate}
            disabled={!tenant?.isActive || tenant?.status !== 'Active'}
          >
            Impersonate Tenant
          </Button>
          <Button
            icon={<KeyOutlined />}
            onClick={handleGetAdminSetupLink}
            disabled={!tenant?.adminEmail}
          >
            Get Admin Setup Link
          </Button>
          {tenant?.status === 'Active' ? (
            <Button danger onClick={handleSuspend}>
              Suspend Tenant
            </Button>
          ) : (
            <Button type="primary" onClick={handleResume}>
              Resume Tenant
            </Button>
          )}
        </Space>
      </Card>

      <Card style={{ marginTop: 16 }}>
        <Tabs activeKey={activeTab} onChange={setActiveTab}>
          <Tabs.TabPane tab="Overview" key="overview">
            <Row gutter={16}>
              <Col span={12}>
                <Card title="Current Plan" size="small">
                  {tenant?.subscription ? (
                    <div>
                      <p><strong>Plan:</strong> {tenant.subscription.planName}</p>
                      <p><strong>Status:</strong> <Tag color={tenant.subscription.status === 'Active' ? 'green' : 'orange'}>{tenant.subscription.status}</Tag></p>
                      <p><strong>Price:</strong> ${tenant.subscription.price.toFixed(2)} / {tenant.subscription.billingPeriod}</p>
                      <p><strong>Period:</strong> {new Date(tenant.subscription.startDate).toLocaleDateString()} - {tenant.subscription.endDate ? new Date(tenant.subscription.endDate).toLocaleDateString() : 'Ongoing'}</p>
                    </div>
                  ) : (
                    <p>No active subscription</p>
                  )}
                </Card>
              </Col>
              <Col span={12}>
                <Card title="Usage Metrics" size="small">
                  {tenant?.usageMetrics ? (
                    <div>
                      <p><strong>API Requests:</strong> {tenant.usageMetrics.apiRequestCount.toLocaleString()}</p>
                      <p><strong>Users:</strong> {tenant.usageMetrics.userCount}</p>
                      <p><strong>Employees:</strong> {tenant.usageMetrics.employeeCount}</p>
                      <p><strong>Last Updated:</strong> {new Date(tenant.usageMetrics.lastUpdated).toLocaleString()}</p>
                    </div>
                  ) : (
                    <p>No usage metrics available</p>
                  )}
                </Card>
              </Col>
            </Row>

            <Card title="Usage Trends" style={{ marginTop: 16 }}>
              {usageChartData.length > 0 ? (
                <div style={{ height: 300, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <p>Chart visualization coming soon. API Request Count: {tenant?.usageMetrics?.apiRequestCount || 0}</p>
                </div>
              ) : (
                <p>No usage data available</p>
              )}
            </Card>
          </Tabs.TabPane>

          <Tabs.TabPane tab="Subscription History" key="subscriptions">
            <Table
              columns={subscriptionColumns}
              dataSource={(tenant?.subscriptions || []) as SubscriptionItem[]}
              rowKey="id"
              pagination={false}
            />
          </Tabs.TabPane>

          <Tabs.TabPane tab="Lifecycle Events" key="events">
            <Timeline>
              {(tenant?.recentLifecycleEvents || []).map((event: any, index: number) => (
                <Timeline.Item key={index} color={event.eventType === 'Activated' || event.EventType === 'Activated' ? 'green' : 'blue'}>
                  <p><strong>{event.eventType || event.EventType}</strong> - {new Date(event.eventDate || event.EventDate).toLocaleString()}</p>
                  {(event.description || event.Description) && <p>{event.description || event.Description}</p>}
                </Timeline.Item>
              ))}
            </Timeline>
          </Tabs.TabPane>

          <Tabs.TabPane tab="Audit Logs (Last 100)" key="logs">
            <Table
              columns={logColumns}
              dataSource={logs}
              rowKey="id"
              loading={logsLoading}
              pagination={{ pageSize: 20 }}
            />
          </Tabs.TabPane>
        </Tabs>
      </Card>
    </div>
  );
}

