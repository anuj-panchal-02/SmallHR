import { useState, useEffect } from 'react';
import { Card, Table, Input, Select, Tag, Space, Button, Row, Col, Statistic, Modal, Form, InputNumber } from 'antd';
import {
  SearchOutlined,
  EyeOutlined,
  UserSwitchOutlined,
  PlusOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';
import { useNotification } from '../contexts/NotificationContext';
import ImpersonationBanner from '../components/ImpersonationBanner';

interface Tenant {
  id: number;
  name: string;
  domain: string | null;
  status: string;
  isActive: boolean;
  isSubscriptionActive: boolean;
  adminEmail: string | null;
  subscription: {
    planName: string;
    status: string;
    currentPeriodEnd: string | null;
    price: number;
    billingPeriod: string;
  } | null;
  userCount: number;
  employeeCount: number;
  createdAt: string;
  updatedAt: string;
}

export default function TenantsList() {
  const navigate = useNavigate();
  const notify = useNotification();
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [loading, setLoading] = useState(false);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string | undefined>();
  const [subscriptionStatusFilter, setSubscriptionStatusFilter] = useState<string | undefined>();
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  
  // Impersonation state
  const [isImpersonating, setIsImpersonating] = useState(false);
  const [impersonatedTenant, setImpersonatedTenant] = useState<{ id: number; name: string; expiresAt: string } | null>(null);
  const [impersonateModalVisible, setImpersonateModalVisible] = useState(false);
  const [selectedTenant, setSelectedTenant] = useState<Tenant | null>(null);
  
  // Create tenant state
  const [createTenantModalVisible, setCreateTenantModalVisible] = useState(false);
  const [createTenantForm] = Form.useForm();
  const [creatingTenant, setCreatingTenant] = useState(false);
  const [subscriptionPlans, setSubscriptionPlans] = useState<Array<{ name: string; maxEmployees: number; price: number }>>([]);

  useEffect(() => {
    fetchTenants();
    checkImpersonationStatus();
    fetchSubscriptionPlans();
  }, [currentPage, pageSize, search, statusFilter, subscriptionStatusFilter, sortBy, sortOrder]);

  const fetchSubscriptionPlans = async () => {
    try {
      const response = await api.get('/tenants/subscription-plans');
      setSubscriptionPlans(response.data || []);
    } catch (error) {
      console.error('Failed to fetch subscription plans:', error);
      // Set default plans if API fails
      setSubscriptionPlans([
        { name: 'Free', maxEmployees: 10, price: 0 },
        { name: 'Basic', maxEmployees: 50, price: 99 },
        { name: 'Pro', maxEmployees: 200, price: 299 },
        { name: 'Enterprise', maxEmployees: 1000, price: 999 },
      ]);
    }
  };

  const handleCreateTenant = () => {
    createTenantForm.resetFields();
    createTenantForm.setFieldsValue({
      subscriptionPlan: 'Free',
      maxEmployees: 10,
    });
    setCreateTenantModalVisible(true);
  };

  const confirmCreateTenant = async () => {
    try {
      const values = await createTenantForm.validateFields();
      setCreatingTenant(true);

      const request = {
        name: values.name,
        domain: values.domain || null,
        adminEmail: values.adminEmail,
        adminFirstName: values.adminFirstName || null,
        adminLastName: values.adminLastName || null,
        subscriptionPlan: values.subscriptionPlan || 'Free',
        maxEmployees: values.maxEmployees || 10,
        isActive: true,
      };

      const response = await api.post('/tenants', request);
      const tenantId = response.data?.id;
      
      notify.success('Tenant Created', `Tenant "${values.name}" has been created and provisioning has started.`);
      
      // Wait a moment for provisioning to start, then fetch setup link with retry logic
      const fetchSetupLinkWithRetry = async (retries = 3, delay = 3000) => {
        for (let i = 0; i < retries; i++) {
          try {
            if (tenantId) {
              const setupLinkResponse = await api.get(`/admin/tenants/${tenantId}/admin-setup-link`);
              const setupLink = setupLinkResponse.data?.setupLink;
              const adminEmail = setupLinkResponse.data?.adminEmail;
              
              if (setupLink) {
                // Show setup link in a modal
                Modal.info({
                  title: 'Admin Setup Link',
                  width: 700,
                  content: (
                    <div>
                      <p><strong>Tenant:</strong> {values.name}</p>
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
                return; // Success, exit retry loop
              }
            }
          } catch (error: any) {
            const isProvisioning = error.response?.status === 503;
            const isNotFound = error.response?.status === 404;
            
            if (i < retries - 1 && (isProvisioning || isNotFound)) {
              // Wait before retrying
              await new Promise(resolve => setTimeout(resolve, delay));
              continue;
            } else {
              // Show message if all retries failed
              const message = isProvisioning 
                ? 'Provisioning is still in progress. You can get the setup link from the tenant detail page once provisioning completes.'
                : 'Could not fetch setup link. The tenant may still be provisioning. You can get the setup link from the tenant detail page later.';
              
              notify.warning('Setup Link Not Available', message);
              console.log('Could not fetch setup link after retries:', error);
            }
          }
        }
      };

      // Start fetching after 3 seconds (give provisioning time to start)
      setTimeout(() => {
        fetchSetupLinkWithRetry(3, 3000); // 3 retries, 3 seconds between each
      }, 3000);
      
      setCreateTenantModalVisible(false);
      createTenantForm.resetFields();
      
      // Refresh tenant list
      fetchTenants();
      
      // Navigate to tenant detail if ID is returned
      if (tenantId) {
        setTimeout(() => {
          navigate(`/admin/tenants/${tenantId}`);
        }, 1000);
      }
    } catch (error: any) {
      if (error.response?.status === 409) {
        notify.error('Domain Conflict', error.response?.data?.message || 'This domain is already in use.');
      } else {
        notify.error('Failed to Create Tenant', error.response?.data?.message || 'Unable to create tenant.');
      }
      console.error(error);
    } finally {
      setCreatingTenant(false);
    }
  };

  const checkImpersonationStatus = () => {
    const impersonationToken = localStorage.getItem('impersonationToken');
    const impersonatedTenantData = localStorage.getItem('impersonatedTenant');
    
    if (impersonationToken && impersonatedTenantData) {
      try {
        const tenant = JSON.parse(impersonatedTenantData);
        const expiresAt = new Date(tenant.expiresAt || new Date().getTime() + 30 * 60 * 1000).toISOString();
        setIsImpersonating(true);
        setImpersonatedTenant({
          id: tenant.id,
          name: tenant.name,
          expiresAt: expiresAt,
        });
      } catch (e) {
        console.error('Error parsing impersonation data:', e);
      }
    }
  };

  const fetchTenants = async () => {
    setLoading(true);
    try {
      const params: any = {
        pageNumber: currentPage,
        pageSize: pageSize,
        sortBy: sortBy,
        sortOrder: sortOrder,
      };

      if (search) params.search = search;
      if (statusFilter) params.status = statusFilter;
      if (subscriptionStatusFilter) params.subscriptionStatus = subscriptionStatusFilter;

      const response = await api.get('/admin/tenants', { params });
      setTenants(response.data.tenants || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (error: any) {
      notify.error('Failed to Load Tenants', error.response?.data?.message || 'Unable to fetch tenant list.');
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const handleImpersonate = (tenant: Tenant) => {
    setSelectedTenant(tenant);
    setImpersonateModalVisible(true);
  };

  const confirmImpersonate = async () => {
    if (!selectedTenant) return;

    try {
      const response = await api.post(`/admin/tenants/${selectedTenant.id}/impersonate?durationMinutes=30`);
      
      // Store impersonation token
      localStorage.setItem('impersonationToken', response.data.impersonationToken);
      localStorage.setItem('impersonatedTenant', JSON.stringify({
        id: response.data.tenant.id,
        name: response.data.tenant.name,
        expiresAt: response.data.expiresAt,
      }));

      // Update API client to use impersonation token
      api.defaults.headers.common['Authorization'] = `Bearer ${response.data.impersonationToken}`;

      setIsImpersonating(true);
      setImpersonatedTenant({
        id: response.data.tenant.id,
        name: response.data.tenant.name,
        expiresAt: response.data.expiresAt,
      });

      notify.success('Impersonation Started', response.data.banner);
      setImpersonateModalVisible(false);
      
      // Refresh tenant list with new token
      fetchTenants();
    } catch (error: any) {
      notify.error('Impersonation Failed', error.response?.data?.message || 'Unable to impersonate tenant.');
      console.error(error);
    }
  };

  const handleStopImpersonation = async () => {
    try {
      await api.post('/admin/tenants/stop-impersonation');
      
      // Clear impersonation token
      localStorage.removeItem('impersonationToken');
      localStorage.removeItem('impersonatedTenant');
      
      // Restore SuperAdmin token (you'll need to store it separately)
      // For now, just remove the impersonation token
      delete api.defaults.headers.common['Authorization'];
      
      setIsImpersonating(false);
      setImpersonatedTenant(null);
      
      // Refresh tenant list
      fetchTenants();
    } catch (error: any) {
      notify.error('Error', 'Failed to stop impersonation.');
      console.error(error);
    }
  };

  const getStatusTag = (status: string) => {
    const statusConfig: Record<string, { color: string; label: string }> = {
      Active: { color: 'green', label: 'Active' },
      Suspended: { color: 'red', label: 'Suspended' },
      Provisioning: { color: 'blue', label: 'Provisioning' },
      Cancelled: { color: 'orange', label: 'Cancelled' },
      Deleted: { color: 'default', label: 'Deleted' },
    };

    const config = statusConfig[status] || { color: 'default', label: status };
    return <Tag color={config.color}>{config.label}</Tag>;
  };

  const columns: ColumnsType<Tenant> = [
    {
      title: 'Name',
      dataIndex: 'name',
      key: 'name',
      sorter: sortBy === 'name',
      render: (text: string, record: Tenant) => (
        <Button
          type="link"
          onClick={() => navigate(`/admin/tenants/${record.id}`)}
          style={{ padding: 0 }}
        >
          {text}
        </Button>
      ),
    },
    {
      title: 'Domain',
      dataIndex: 'domain',
      key: 'domain',
      render: (text: string | null) => text || <Tag color="default">Not Set</Tag>,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      sorter: sortBy === 'status',
      render: (status: string) => getStatusTag(status),
    },
    {
      title: 'Plan',
      key: 'plan',
      render: (_: any, record: Tenant) => (
        record.subscription ? (
          <Tag color="blue">{record.subscription.planName}</Tag>
        ) : (
          <Tag color="default">No Plan</Tag>
        )
      ),
    },
    {
      title: 'Users',
      dataIndex: 'userCount',
      key: 'userCount',
      align: 'center',
      render: (count: number) => <Statistic value={count} valueStyle={{ fontSize: 14 }} />,
    },
    {
      title: 'Employees',
      dataIndex: 'employeeCount',
      key: 'employeeCount',
      align: 'center',
      render: (count: number) => <Statistic value={count} valueStyle={{ fontSize: 14 }} />,
    },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      key: 'createdAt',
      sorter: sortBy === 'createdAt',
      render: (date: string) => new Date(date).toLocaleDateString(),
    },
    {
      title: 'Actions',
      key: 'actions',
      width: 150,
      render: (_: any, record: Tenant) => (
        <Space>
          <Button
            icon={<EyeOutlined />}
            onClick={() => navigate(`/admin/tenants/${record.id}`)}
          >
            View
          </Button>
          <Button
            icon={<UserSwitchOutlined />}
            onClick={() => handleImpersonate(record)}
            disabled={!record.isActive || record.status !== 'Active'}
          >
            Impersonate
          </Button>
        </Space>
      ),
    },
  ];

  const handleTableChange = (pagination: any, _filters: any, sorter: any) => {
    if (sorter.field) {
      setSortBy(sorter.field);
      setSortOrder(sorter.order === 'ascend' ? 'asc' : 'desc');
    }
    setCurrentPage(pagination.current);
    setPageSize(pagination.pageSize);
  };

  return (
    <div>
      {isImpersonating && impersonatedTenant && (
        <ImpersonationBanner
          tenantId={impersonatedTenant.id}
          tenantName={impersonatedTenant.name}
          expiresAt={impersonatedTenant.expiresAt}
          onStopImpersonation={handleStopImpersonation}
        />
      )}

      <div style={{ marginBottom: 24 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
          <h2 style={{ margin: 0 }}>Tenant Management</h2>
          <Button 
            type="primary" 
            icon={<PlusOutlined />}
            onClick={handleCreateTenant}
          >
            Create Tenant
          </Button>
        </div>
      </div>

      <Card>
        <Row gutter={16} style={{ marginBottom: 16 }}>
          <Col span={8}>
            <Input
              placeholder="Search by name, domain, or email..."
              prefix={<SearchOutlined />}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              allowClear
            />
          </Col>
          <Col span={4}>
            <Select
              placeholder="Status"
              style={{ width: '100%' }}
              allowClear
              value={statusFilter}
              onChange={setStatusFilter}
            >
              <Select.Option value="Active">Active</Select.Option>
              <Select.Option value="Suspended">Suspended</Select.Option>
              <Select.Option value="Provisioning">Provisioning</Select.Option>
              <Select.Option value="Cancelled">Cancelled</Select.Option>
            </Select>
          </Col>
          <Col span={4}>
            <Select
              placeholder="Subscription"
              style={{ width: '100%' }}
              allowClear
              value={subscriptionStatusFilter}
              onChange={setSubscriptionStatusFilter}
            >
              <Select.Option value="Active">Active</Select.Option>
              <Select.Option value="Trialing">Trialing</Select.Option>
              <Select.Option value="PastDue">Past Due</Select.Option>
              <Select.Option value="Canceled">Canceled</Select.Option>
            </Select>
          </Col>
          <Col span={8}>
            <Button onClick={fetchTenants} loading={loading}>
              Refresh
            </Button>
          </Col>
        </Row>

        <Table
          columns={columns}
          dataSource={tenants}
          rowKey="id"
          loading={loading}
          pagination={{
            current: currentPage,
            pageSize: pageSize,
            total: totalCount,
            showSizeChanger: true,
            showTotal: (total) => `Total ${total} tenants`,
          }}
          onChange={handleTableChange}
        />
      </Card>

      <Modal
        title="Impersonate Tenant"
        open={impersonateModalVisible}
        onOk={confirmImpersonate}
        onCancel={() => setImpersonateModalVisible(false)}
        okText="Impersonate"
        okButtonProps={{ danger: true }}
      >
        <p>
          <strong>Warning:</strong> You are about to impersonate <strong>{selectedTenant?.name}</strong>.
        </p>
        <p>This will allow you to view and act as this tenant for 30 minutes. All your actions will be logged in the audit trail.</p>
        <p>Do you want to continue?</p>
      </Modal>

      <Modal
        title="Create New Tenant"
        open={createTenantModalVisible}
        onOk={confirmCreateTenant}
        onCancel={() => setCreateTenantModalVisible(false)}
        okText="Create Tenant"
        okButtonProps={{ loading: creatingTenant }}
        width={600}
      >
        <Form
          form={createTenantForm}
          layout="vertical"
          onFinish={confirmCreateTenant}
        >
          <Form.Item
            name="name"
            label="Tenant Name"
            rules={[{ required: true, message: 'Tenant name is required' }]}
          >
            <Input placeholder="Enter tenant name" />
          </Form.Item>

          <Form.Item
            name="domain"
            label="Domain (Optional)"
            tooltip="Subdomain for tenant access (e.g., acme.smallhr.com)"
          >
            <Input placeholder="e.g., acme" />
          </Form.Item>

          <Form.Item
            name="adminEmail"
            label="Admin Email"
            rules={[
              { required: true, message: 'Admin email is required' },
              { type: 'email', message: 'Please enter a valid email' }
            ]}
          >
            <Input type="email" placeholder="admin@example.com" />
          </Form.Item>

          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                name="adminFirstName"
                label="Admin First Name"
              >
                <Input placeholder="First name" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="adminLastName"
                label="Admin Last Name"
              >
                <Input placeholder="Last name" />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item
            name="subscriptionPlan"
            label="Subscription Plan"
            rules={[{ required: true, message: 'Subscription plan is required' }]}
          >
            <Select 
              placeholder="Select a plan"
              onChange={(value) => {
                const selectedPlan = subscriptionPlans.find(p => p.name === value);
                if (selectedPlan) {
                  createTenantForm.setFieldsValue({ maxEmployees: selectedPlan.maxEmployees });
                }
              }}
            >
              {subscriptionPlans.map(plan => (
                <Select.Option key={plan.name} value={plan.name}>
                  {plan.name} - {plan.maxEmployees} employees (${plan.price}/month)
                </Select.Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item
            name="maxEmployees"
            label="Max Employees"
            rules={[{ required: true, message: 'Max employees is required' }]}
          >
            <InputNumber
              min={1}
              max={10000}
              style={{ width: '100%' }}
              placeholder="Maximum number of employees"
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

