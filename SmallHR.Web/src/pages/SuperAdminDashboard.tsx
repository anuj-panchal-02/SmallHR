import { useState, useEffect } from 'react';
import { Card, Table, Button, Modal, Form, Input, Select, Tag, Space, Popconfirm, Row, Col, Statistic, DatePicker, message } from 'antd';
import { useNotification } from '../contexts/NotificationContext';
import dayjs from 'dayjs';
import {
  UserAddOutlined,
  EditOutlined,
  LockOutlined,
  PoweroffOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  TeamOutlined,
  CrownOutlined,
  UserOutlined,
  PlusCircleOutlined,
  ApartmentOutlined,
  DollarOutlined,
  BellOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import PageHeader from '../components/PageHeader';
import api from '../services/api';
import { useNavigate } from 'react-router-dom';

interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  createdAt: string;
  roles: string[];
}

export default function SuperAdminDashboard() {
  const navigate = useNavigate();
  const notify = useNotification();
  const [users, setUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [isCreateModalVisible, setIsCreateModalVisible] = useState(false);
  const [isRoleModalVisible, setIsRoleModalVisible] = useState(false);
  const [isPasswordModalVisible, setIsPasswordModalVisible] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [form] = Form.useForm();
  const [roleForm] = Form.useForm();
  const [passwordForm] = Form.useForm();

  useEffect(() => {
    fetchUsers();
    fetchRoles();
  }, []);

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const response = await api.get('/usermanagement/users');
      setUsers(response.data);
    } catch (error: any) {
      notify.error('Failed to Load Users', 'Unable to fetch user list. Please try again.');
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const fetchRoles = async () => {
    try {
      const response = await api.get('/usermanagement/roles');
      setRoles(response.data);
    } catch (error: any) {
      console.error('Failed to fetch roles', error);
    }
  };

  const handleCreateUser = async (values: any) => {
    try {
      console.log('📝 Creating user with values:', values);
      
      // Convert dateOfBirth from dayjs to ISO string
      const payload = {
        ...values,
        dateOfBirth: values.dateOfBirth ? dayjs(values.dateOfBirth).toISOString() : new Date().toISOString(),
      };
      
      const response = await api.post('/usermanagement/create-user', payload);
      
      console.log('✅ User created:', response.data);
      notify.success('User Created Successfully', `${values.firstName} ${values.lastName} has been added to the system.`);
      setIsCreateModalVisible(false);
      form.resetFields();
      fetchUsers();
    } catch (error: any) {
      console.error('❌ Create user error:', error);
      console.error('❌ Error response:', error.response?.data);
      
      const errorResponse = error.response?.data;
      let errorMsg = 'Failed to create user';
      
      if (errorResponse?.errors && Array.isArray(errorResponse.errors)) {
        // Join all validation errors for better visibility
        errorMsg = errorResponse.errors.join('. ');
      } else if (errorResponse?.message) {
        errorMsg = errorResponse.message;
      } else if (errorResponse?.error) {
        errorMsg = errorResponse.error;
      }
      
      console.error('User creation errors:', errorResponse);
      notify.error('User Creation Failed', errorMsg);
    }
  };

  const handleQuickCreateAllRoles = async () => {
    const demoUsers = [
      { email: 'admin@smallhr.com', password: 'Admin@123', role: 'Admin', firstName: 'Admin', lastName: 'User', dateOfBirth: dayjs('1990-05-15') },
      { email: 'hr@smallhr.com', password: 'Hr@123', role: 'HR', firstName: 'HR', lastName: 'Manager', dateOfBirth: dayjs('1988-08-20') },
      { email: 'employee@smallhr.com', password: 'Employee@123', role: 'Employee', firstName: 'John', lastName: 'Employee', dateOfBirth: dayjs('1995-03-10') }
    ];

    setLoading(true);
    let successCount = 0;
    let failCount = 0;
    const errors: string[] = [];

    try {
      // Fetch existing users once to check for duplicates
      const existingUsersResponse = await api.get('/usermanagement/users');
      const existingEmails = existingUsersResponse.data.map((u: User) => u.email);

      for (const user of demoUsers) {
        try {
          // Check if user already exists
          if (existingEmails.includes(user.email)) {
            message.warning(`${user.email} already exists, skipping...`);
            continue;
          }

          const payload = {
            ...user,
            dateOfBirth: user.dateOfBirth.toISOString(),
          };
          
          await api.post('/usermanagement/create-user', payload);
          successCount++;
          message.success(`${user.email} created successfully`);
        } catch (error: any) {
          failCount++;
          const errorMsg = error.response?.data?.message || 'Failed to create user';
          errors.push(`${user.email}: ${errorMsg}`);
          message.error(`Failed to create ${user.email}`);
          console.error(`Failed to create ${user.email}:`, error);
        }
      }

      if (successCount > 0) {
        notify.success('Users Created', `${successCount} user(s) created successfully.`);
      }
      if (failCount > 0 && successCount === 0) {
        notify.error('All Users Failed', errors.join('\n'));
      } else if (failCount > 0) {
        notify.warning('Partial Success', `${successCount} created, ${failCount} failed.`);
      }
    } catch (error: any) {
      notify.error('Error', 'An error occurred while creating users.');
      console.error('Quick create error:', error);
    } finally {
      setLoading(false);
      fetchUsers();
    }
  };

  const handleUpdateRole = async (values: any) => {
    if (!selectedUser) return;
    try {
      await api.put(`/usermanagement/update-role/${selectedUser.id}`, values);
      notify.success('Role Updated', `${selectedUser.firstName}'s role has been changed to ${values.role}`);
      setIsRoleModalVisible(false);
      roleForm.resetFields();
      fetchUsers();
    } catch (error: any) {
      notify.error('Role Update Failed', 'Unable to update user role. Please try again.');
    }
  };

  const handleResetPassword = async (values: any) => {
    if (!selectedUser) return;
    try {
      await api.post(`/usermanagement/reset-password/${selectedUser.id}`, values);
      notify.success('Password Reset', `Password has been reset for ${selectedUser.email}`);
      setIsPasswordModalVisible(false);
      passwordForm.resetFields();
    } catch (error: any) {
      notify.error('Password Reset Failed', 'Unable to reset password. Please try again.');
    }
  };

  const handleToggleStatus = async (userId: string) => {
    const user = users.find(u => u.id === userId);
    try {
      await api.put(`/usermanagement/toggle-status/${userId}`, null);
      const action = user?.isActive ? 'deactivated' : 'activated';
      notify.success('Status Updated', `User has been ${action}`);
      fetchUsers();
    } catch (error: any) {
      notify.error('Status Update Failed', 'Unable to update user status. Please try again.');
    }
  };

  const columns: ColumnsType<User> = [
    {
      title: 'Name',
      key: 'name',
      width: 180,
      render: (_, record) => `${record.firstName} ${record.lastName}`,
    },
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
      width: 220,
    },
    {
      title: 'Role',
      dataIndex: 'roles',
      key: 'roles',
      width: 150,
      render: (roles: string[]) => (
        <>
          {roles.map((role) => {
            let color = 'blue';
            if (role === 'SuperAdmin') color = 'purple';
            if (role === 'Admin') color = 'red';
            if (role === 'HR') color = 'orange';
            if (role === 'Employee') color = 'green';
            return (
              <Tag key={role} color={color}>
                {role}
              </Tag>
            );
          })}
        </>
      ),
    },
    {
      title: 'Status',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 100,
      align: 'center' as const,
      render: (isActive: boolean) =>
        isActive ? (
          <Tag icon={<CheckCircleOutlined />} color="success">
            Active
          </Tag>
        ) : (
          <Tag icon={<CloseCircleOutlined />} color="error">
            Inactive
          </Tag>
        ),
    },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 120,
      render: (date: string) => new Date(date).toLocaleDateString(),
    },
    {
      title: 'Actions',
      key: 'actions',
      width: 200,
      fixed: 'right' as const,
      render: (_, record) => (
        <Space size="small">
          <Button
            type="link"
            icon={<EditOutlined />}
            onClick={() => {
              setSelectedUser(record);
              roleForm.setFieldsValue({ role: record.roles[0] });
              setIsRoleModalVisible(true);
            }}
          >
            Change Role
          </Button>
          <Button
            type="link"
            icon={<LockOutlined />}
            onClick={() => {
              setSelectedUser(record);
              setIsPasswordModalVisible(true);
            }}
          >
            Reset Password
          </Button>
          <Popconfirm
            title={`${record.isActive ? 'Deactivate' : 'Activate'} user?`}
            onConfirm={() => handleToggleStatus(record.id)}
          >
            <Button
              type="link"
              icon={<PoweroffOutlined />}
              danger={record.isActive}
            >
              {record.isActive ? 'Deactivate' : 'Activate'}
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  // Calculate statistics
  const totalUsers = users.length;
  const activeUsers = users.filter(u => u.isActive).length;
  const superAdmins = users.filter(u => u.roles.includes('SuperAdmin')).length;
  const admins = users.filter(u => u.roles.includes('Admin')).length;
  const hrs = users.filter(u => u.roles.includes('HR')).length;
  const employees = users.filter(u => u.roles.includes('Employee')).length;

  return (
    <div>
      <PageHeader title="Super Admin Control Panel" subtitle="Full System Access & Control" />

      {/* System Statistics */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} md={6}>
          <Card
            bordered={false}
            style={{
              borderRadius: 'var(--card-radius)',
              boxShadow: 'var(--card-shadow)',
              background: 'linear-gradient(135deg, #8B5CF6 0%, #7C3AED 100%)',
            }}
            bodyStyle={{ padding: 'var(--card-padding)' }}
          >
            <Statistic
              title={
                <span style={{ color: 'var(--color-text-secondary, #64748B)', fontFamily: 'Inter, sans-serif', fontWeight: 500 }}>
                  Total Users
                </span>
              }
              value={totalUsers}
              valueStyle={{ color: 'var(--color-text-primary, #1E293B)', fontFamily: 'Inter, sans-serif', fontWeight: 700 }}
              prefix={<TeamOutlined style={{ color: 'var(--color-primary, #4F46E5)' }} />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card
            bordered={false}
            style={{
              borderRadius: 'var(--card-radius)',
              boxShadow: 'var(--card-shadow)',
              background: 'linear-gradient(135deg, #10B981 0%, #059669 100%)',
            }}
            bodyStyle={{ padding: 'var(--card-padding)' }}
          >
            <Statistic
              title={
                <span style={{ color: 'var(--color-text-secondary, #64748B)', fontFamily: 'Inter, sans-serif', fontWeight: 500 }}>
                  Active Users
                </span>
              }
              value={activeUsers}
              valueStyle={{ color: 'var(--color-text-primary, #1E293B)', fontFamily: 'Inter, sans-serif', fontWeight: 700 }}
              prefix={<CheckCircleOutlined style={{ color: 'var(--color-success, #10B981)' }} />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card
            bordered={false}
            style={{
              borderRadius: 'var(--card-radius)',
              boxShadow: 'var(--card-shadow)',
              background: 'linear-gradient(135deg, #F59E0B 0%, #D97706 100%)',
            }}
            bodyStyle={{ padding: 'var(--card-padding)' }}
          >
            <Statistic
              title={
                <span style={{ color: 'var(--color-text-secondary, #64748B)', fontFamily: 'Inter, sans-serif', fontWeight: 500 }}>
                  Super Admins
                </span>
              }
              value={superAdmins}
              valueStyle={{ color: 'var(--color-text-primary, #1E293B)', fontFamily: 'Inter, sans-serif', fontWeight: 700 }}
              prefix={<CrownOutlined style={{ color: 'var(--color-warning, #F59E0B)' }} />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card
            bordered={false}
            style={{
              borderRadius: 'var(--card-radius)',
              boxShadow: 'var(--card-shadow)',
              background: 'linear-gradient(135deg, #3B82F6 0%, #2563EB 100%)',
            }}
            bodyStyle={{ padding: 'var(--card-padding)' }}
          >
            <Statistic
              title={
                <span style={{ color: 'var(--color-text-secondary, #64748B)', fontFamily: 'Inter, sans-serif', fontWeight: 500 }}>
                  Staff (A/H/E)
                </span>
              }
              value={`${admins}/${hrs}/${employees}`}
              valueStyle={{ color: 'var(--color-text-primary, #1E293B)', fontFamily: 'Inter, sans-serif', fontWeight: 700, fontSize: 28 }}
              prefix={<UserOutlined style={{ color: 'var(--color-accent, #06B6D4)' }} />}
            />
          </Card>
        </Col>
      </Row>

      {/* Quick Access Cards */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} md={8}>
          <Card
            hoverable
            onClick={() => navigate('/admin/tenants')}
            bordered={false}
            style={{
              borderRadius: 20,
              boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)',
              cursor: 'pointer',
              transition: 'all 0.3s ease',
            }}
          >
            <div style={{ textAlign: 'center' }}>
              <ApartmentOutlined style={{ fontSize: 48, color: '#4F46E5', marginBottom: 16 }} />
              <h3 style={{ margin: 0, marginBottom: 8 }}>Tenants</h3>
              <p style={{ margin: 0, color: '#64748B' }}>Manage tenants, view details, impersonate</p>
            </div>
          </Card>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <Card
            hoverable
            onClick={() => navigate('/admin/billing')}
            bordered={false}
            style={{
              borderRadius: 20,
              boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)',
              cursor: 'pointer',
              transition: 'all 0.3s ease',
            }}
          >
            <div style={{ textAlign: 'center' }}>
              <DollarOutlined style={{ fontSize: 48, color: '#10B981', marginBottom: 16 }} />
              <h3 style={{ margin: 0, marginBottom: 8 }}>Billing Center</h3>
              <p style={{ margin: 0, color: '#64748B' }}>View webhooks, reconcile payments</p>
            </div>
          </Card>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <Card
            hoverable
            onClick={() => navigate('/admin/alerts')}
            bordered={false}
            style={{
              borderRadius: 20,
              boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)',
              cursor: 'pointer',
              transition: 'all 0.3s ease',
            }}
          >
            <div style={{ textAlign: 'center' }}>
              <BellOutlined style={{ fontSize: 48, color: '#EF4444', marginBottom: 16 }} />
              <h3 style={{ margin: 0, marginBottom: 8 }}>Alerts Hub</h3>
              <p style={{ margin: 0, color: '#64748B' }}>Payment failures, overages, errors</p>
            </div>
          </Card>
        </Col>
      </Row>

      {/* User Management Card */}
      <Card
        title={
          <span
            style={{
              fontSize: 18,
              fontWeight: 600,
              color: '#1E293B',
              fontFamily: 'Inter, sans-serif',
              letterSpacing: '-0.01em',
            }}
          >
            User Management
          </span>
        }
        bordered={false}
        style={{
          borderRadius: 20,
          boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)',
        }}
        bodyStyle={{ padding: '24px 32px 32px' }}
        extra={
          <Space>
            <Button
              icon={<PlusCircleOutlined />}
              onClick={handleQuickCreateAllRoles}
              loading={loading}
              style={{
                borderRadius: 'var(--button-radius)',
                height: 'var(--button-height)',
              }}
            >
              Quick Create All Roles
            </Button>
            <Button
              type="primary"
              icon={<UserAddOutlined />}
              onClick={() => setIsCreateModalVisible(true)}
              style={{
                borderRadius: 'var(--button-radius)',
                height: 'var(--button-height)',
                border: 'none',
                background: 'var(--gradient-primary)',
                boxShadow: 'var(--button-shadow-primary)',
                transition: 'all 250ms ease-in-out',
                fontWeight: 'var(--button-font-weight)',
                fontFamily: 'var(--button-font-family)',
              }}
            >
              Create New User
            </Button>
          </Space>
        }
      >
        <Table
          columns={columns}
          dataSource={users}
          rowKey="id"
          loading={loading}
          pagination={{
            pageSize: 10,
            showSizeChanger: false,
            showTotal: (total) => (
              <span
                style={{
                  color: '#64748B',
                  fontFamily: 'Inter, sans-serif',
                  fontWeight: 500,
                  fontSize: 13,
                }}
              >
                Total {total} users
              </span>
            ),
            style: { marginTop: 24 },
          }}
          size="middle"
          style={{ marginTop: 16 }}
        />
      </Card>

      {/* Create User Modal */}
      <Modal
        title="Create New User"
        open={isCreateModalVisible}
        onCancel={() => {
          setIsCreateModalVisible(false);
          form.resetFields();
        }}
        footer={null}
        width={600}
      >
        <Form form={form} layout="vertical" onFinish={handleCreateUser}>
          <Form.Item
            name="email"
            label="Email"
            rules={[
              { required: true, message: 'Please enter email' },
              { type: 'email', message: 'Please enter valid email' },
            ]}
          >
            <Input placeholder="user@smallhr.com" />
          </Form.Item>

          <Form.Item
            name="password"
            label="Password"
            rules={[
              { required: true, message: 'Please enter password' },
              { min: 12, message: 'Password must be at least 12 characters' },
              {
                pattern: /(?=.*[a-z])/,
                message: 'Password must contain at least one lowercase letter'
              },
              {
                pattern: /(?=.*[A-Z])/,
                message: 'Password must contain at least one uppercase letter'
              },
              {
                pattern: /(?=.*\d)/,
                message: 'Password must contain at least one number'
              },
              {
                pattern: /(?=.*[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?])/,
                message: 'Password must contain at least one special character'
              },
            ]}
            help="Password must be at least 12 characters with uppercase, lowercase, number, and special character"
          >
            <Input.Password placeholder="Password" />
          </Form.Item>

          <Form.Item
            name="firstName"
            label="First Name"
            rules={[{ required: true, message: 'Please enter first name' }]}
          >
            <Input placeholder="First Name" />
          </Form.Item>

          <Form.Item
            name="lastName"
            label="Last Name"
            rules={[{ required: true, message: 'Please enter last name' }]}
          >
            <Input placeholder="Last Name" />
          </Form.Item>

          <Form.Item
            name="dateOfBirth"
            label="Date of Birth"
            rules={[{ required: true, message: 'Please select date of birth' }]}
          >
            <DatePicker
              style={{ width: '100%' }}
              format="YYYY-MM-DD"
              placeholder="Select date of birth"
              disabledDate={(current) => current && current > dayjs().endOf('day')}
            />
          </Form.Item>

          <Form.Item
            name="role"
            label="Role"
            rules={[{ required: true, message: 'Please select role' }]}
          >
            <Select placeholder="Select role">
              {roles.map((role) => (
                <Select.Option key={role} value={role}>
                  {role}
                </Select.Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item>
            <Space style={{ width: '100%', justifyContent: 'flex-end' }}>
              <Button onClick={() => setIsCreateModalVisible(false)}>Cancel</Button>
              <Button type="primary" htmlType="submit">
                Create User
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* Update Role Modal */}
      <Modal
        title="Update User Role"
        open={isRoleModalVisible}
        onCancel={() => {
          setIsRoleModalVisible(false);
          roleForm.resetFields();
        }}
        footer={null}
      >
        <Form form={roleForm} layout="vertical" onFinish={handleUpdateRole}>
          <p>
            Updating role for: <strong>{selectedUser?.email}</strong>
          </p>
          <Form.Item
            name="role"
            label="Select New Role"
            rules={[{ required: true, message: 'Please select role' }]}
          >
            <Select placeholder="Select role">
              {roles.map((role) => (
                <Select.Option key={role} value={role}>
                  {role}
                </Select.Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item>
            <Space style={{ width: '100%', justifyContent: 'flex-end' }}>
              <Button onClick={() => setIsRoleModalVisible(false)}>Cancel</Button>
              <Button type="primary" htmlType="submit">
                Update Role
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* Reset Password Modal */}
      <Modal
        title="Reset Password"
        open={isPasswordModalVisible}
        onCancel={() => {
          setIsPasswordModalVisible(false);
          passwordForm.resetFields();
        }}
        footer={null}
      >
        <Form form={passwordForm} layout="vertical" onFinish={handleResetPassword}>
          <p>
            Resetting password for: <strong>{selectedUser?.email}</strong>
          </p>
          <Form.Item
            name="newPassword"
            label="New Password"
            rules={[
              { required: true, message: 'Please enter new password' },
              { min: 12, message: 'Password must be at least 12 characters' },
              {
                pattern: /(?=.*[a-z])/,
                message: 'Password must contain at least one lowercase letter'
              },
              {
                pattern: /(?=.*[A-Z])/,
                message: 'Password must contain at least one uppercase letter'
              },
              {
                pattern: /(?=.*\d)/,
                message: 'Password must contain at least one number'
              },
              {
                pattern: /(?=.*[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?])/,
                message: 'Password must contain at least one special character'
              },
            ]}
            help="Password must be at least 12 characters with uppercase, lowercase, number, and special character"
          >
            <Input.Password placeholder="New Password" />
          </Form.Item>

          <Form.Item>
            <Space style={{ width: '100%', justifyContent: 'flex-end' }}>
              <Button onClick={() => setIsPasswordModalVisible(false)}>Cancel</Button>
              <Button type="primary" htmlType="submit">
                Reset Password
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

