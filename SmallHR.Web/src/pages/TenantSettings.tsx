import React, { useState, useEffect } from 'react';
import {
  Table,
  Button,
  Modal,
  Form,
  Input,
  Switch,
  Space,
  Popconfirm,
  Card,
  Tag,
  Typography,
  Select,
  Divider,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, SettingOutlined } from '@ant-design/icons';
import api from '../services/api';
import { useNotification } from '../contexts/NotificationContext';

const { Title } = Typography;

interface Tenant {
  id: number;
  name: string;
  domain: string | null;
  isActive: boolean;
  subscriptionPlan: string;
  maxEmployees: number;
  subscriptionStartDate?: string | null;
  subscriptionEndDate?: string | null;
  isSubscriptionActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

const TenantSettings: React.FC = () => {
  const notify = useNotification();
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [loading, setLoading] = useState(false);
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [editingTenant, setEditingTenant] = useState<Tenant | null>(null);
  const [form] = Form.useForm();

  const subscriptionPlans = [
    { label: 'Free', value: 'Free', maxEmployees: 10 },
    { label: 'Basic', value: 'Basic', maxEmployees: 50 },
    { label: 'Pro', value: 'Pro', maxEmployees: 200 },
    { label: 'Enterprise', value: 'Enterprise', maxEmployees: 1000 },
  ];

  useEffect(() => {
    fetchTenants();
  }, []);

  const fetchTenants = async () => {
    setLoading(true);
    try {
      const response = await api.get('/tenants');
      setTenants(response.data);
    } catch (error: any) {
      console.error('Failed to fetch tenants:', error);
      notify.error('Failed to Load Tenants', 'Unable to fetch tenant list. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingTenant(null);
    form.resetFields();
    setIsModalVisible(true);
  };

  const handleEdit = (tenant: Tenant) => {
    setEditingTenant(tenant);
    form.setFieldsValue(tenant);
    setIsModalVisible(true);
  };

  const handleDelete = async (id: number) => {
    try {
      await api.delete(`/tenants/${id}`);
      notify.success('Tenant Deleted', 'Tenant has been successfully deleted.');
      fetchTenants();
    } catch (error: any) {
      console.error('Failed to delete tenant:', error);
      notify.error('Delete Failed', 'Unable to delete tenant. Please try again.');
    }
  };

  const handleSubmit = async (values: any) => {
    try {
      if (editingTenant) {
        await api.put(`/tenants/${editingTenant.id}`, values);
        notify.success('Tenant Updated', 'Tenant has been successfully updated.');
      } else {
        await api.post('/tenants', values);
        notify.success('Tenant Created', 'Tenant has been successfully created.');
      }
      setIsModalVisible(false);
      fetchTenants();
    } catch (error: any) {
      console.error('Failed to save tenant:', error);
      notify.error(
        editingTenant ? 'Update Failed' : 'Create Failed',
        error.response?.data?.message || 'Unable to save tenant. Please try again.'
      );
    }
  };

  const handleToggleActive = async (tenant: Tenant) => {
    try {
      await api.put(`/tenants/${tenant.id}`, {
        ...tenant,
        isActive: !tenant.isActive,
      });
      notify.success('Status Updated', 'Tenant status has been updated.');
      fetchTenants();
    } catch (error: any) {
      console.error('Failed to toggle active:', error);
      notify.error('Update Failed', 'Unable to update tenant status.');
    }
  };

  const handlePlanChange = (plan: string) => {
    const selectedPlan = subscriptionPlans.find(p => p.value === plan);
    if (selectedPlan) {
      form.setFieldsValue({ maxEmployees: selectedPlan.maxEmployees });
    }
  };

  const columns = [
    {
      title: 'Name',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: 'Domain',
      dataIndex: 'domain',
      key: 'domain',
      render: (text: string | null) => text || '-',
    },
    {
      title: 'Subscription Plan',
      dataIndex: 'subscriptionPlan',
      key: 'subscriptionPlan',
      render: (plan: string) => {
        const colors: Record<string, string> = {
          Free: 'default',
          Basic: 'blue',
          Pro: 'purple',
          Enterprise: 'gold',
        };
        return <Tag color={colors[plan] || 'default'}>{plan}</Tag>;
      },
    },
    {
      title: 'Max Employees',
      dataIndex: 'maxEmployees',
      key: 'maxEmployees',
      render: (count: number) => count === 1000 ? 'Unlimited' : count,
    },
    {
      title: 'Subscription Status',
      key: 'subscriptionStatus',
      render: (_: any, record: Tenant) => (
        <Tag color={record.isSubscriptionActive ? 'success' : 'error'}>
          {record.isSubscriptionActive ? 'Active' : 'Inactive'}
        </Tag>
      ),
    },
    {
      title: 'Status',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (isActive: boolean, record: Tenant) => (
        <Switch
          checked={isActive}
          onChange={() => handleToggleActive(record)}
        />
      ),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: any, record: Tenant) => (
        <Space>
          <Button
            type="link"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
          >
            Edit
          </Button>
          <Popconfirm
            title="Are you sure you want to delete this tenant?"
            onConfirm={() => handleDelete(record.id)}
            okText="Yes"
            cancelText="No"
          >
            <Button type="link" danger icon={<DeleteOutlined />}>
              Delete
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <Card>
        <div style={{ marginBottom: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Title level={3} style={{ margin: 0 }}>
            <SettingOutlined style={{ marginRight: 8 }} />
            Tenant Management
          </Title>
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            Create Tenant
          </Button>
        </div>

        <Table
          columns={columns}
          dataSource={tenants}
          rowKey="id"
          loading={loading}
          pagination={{ pageSize: 10 }}
        />

        <Modal
          title={editingTenant ? 'Edit Tenant' : 'Create Tenant'}
          open={isModalVisible}
          onCancel={() => setIsModalVisible(false)}
          onOk={() => form.submit()}
          width={600}
        >
          <Form form={form} layout="vertical" onFinish={handleSubmit}>
            <Form.Item
              name="name"
              label="Tenant Name"
              rules={[{ required: true, message: 'Please enter tenant name' }]}
            >
              <Input placeholder="Enter tenant name" />
            </Form.Item>

            <Form.Item
              name="domain"
              label="Domain (Optional)"
              rules={[{ type: 'url', message: 'Please enter a valid domain' }]}
            >
              <Input placeholder="e.g., example.com" />
            </Form.Item>

            <Divider>Subscription Plan</Divider>

            <Form.Item
              name="subscriptionPlan"
              label="Plan"
              rules={[{ required: true, message: 'Please select a subscription plan' }]}
            >
              <Select
                placeholder="Select subscription plan"
                onChange={handlePlanChange}
                options={subscriptionPlans.map(p => ({ label: p.label, value: p.value }))}
              />
            </Form.Item>

            <Form.Item
              name="maxEmployees"
              label="Maximum Employees"
              rules={[{ required: true, message: 'Please enter max employees' }]}
            >
              <Input type="number" placeholder="Enter max employees" />
            </Form.Item>

            <Form.Item
              name="isActive"
              label="Active"
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>

            <Form.Item
              name="isSubscriptionActive"
              label="Subscription Active"
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
          </Form>
        </Modal>
      </Card>
    </div>
  );
};

export default TenantSettings;

