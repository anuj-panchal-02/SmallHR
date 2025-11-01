import { useEffect, useState } from 'react';
import { Card, Row, Col, Statistic, Table, Tag, Space, Button } from 'antd';
import {
  UserOutlined,
  TeamOutlined,
  DollarOutlined,
  RiseOutlined,
  CheckCircleOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import PageHeader from '../components/PageHeader';
import api from '../services/api';

interface Employee {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  department: string;
  position: string;
  isActive: boolean;
}

export default function AdminDashboard() {
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchEmployees();
  }, []);

  const fetchEmployees = async () => {
    try {
      const response = await api.get('/employees');
      setEmployees(response.data);
    } catch (error) {
      console.error('Failed to fetch employees:', error);
    } finally {
      setLoading(false);
    }
  };

  const employeeColumns: ColumnsType<Employee> = [
    {
      title: 'Name',
      dataIndex: 'firstName',
      key: 'name',
      width: 180,
      render: (_: string, record: Employee) => `${record.firstName} ${record.lastName}`,
    },
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
      width: 220,
    },
    {
      title: 'Department',
      dataIndex: 'department',
      key: 'department',
      width: 150,
    },
    {
      title: 'Position',
      dataIndex: 'position',
      key: 'position',
      width: 180,
    },
    {
      title: 'Status',
      dataIndex: 'isActive',
      key: 'status',
      width: 100,
      align: 'center' as const,
      render: (isActive: boolean) => (
        <Tag color={isActive ? 'green' : 'red'}>
          {isActive ? 'Active' : 'Inactive'}
        </Tag>
      ),
    },
  ];

  return (
    <div>
      {/* Page Header */}
      <PageHeader title="Admin Dashboard" subtitle="System Overview" />

      {/* Admin Stats - Full System Metrics */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} lg={6}>
          <Card
            bordered={false}
            style={{
              borderRadius: 'var(--card-radius)',
              boxShadow: 'var(--card-shadow)',
            }}
            bodyStyle={{ padding: 'var(--card-padding)' }}
          >
            <Statistic
              title={
                <span style={{ color: 'var(--color-text-secondary)', fontSize: 'var(--stat-title-size)', fontWeight: 'var(--stat-title-weight)', fontFamily: 'var(--button-font-family)' }}>
                  Total Employees
                </span>
              }
              value={employees.length}
              prefix={<TeamOutlined style={{ color: 'var(--icon-color-teal)', fontSize: 'var(--icon-size-lg)' }} />}
              valueStyle={{ color: 'var(--color-text-primary)', fontSize: 'var(--stat-value-size)', fontWeight: 'var(--stat-value-weight)', fontFamily: 'var(--button-font-family)', letterSpacing: 'var(--stat-value-spacing)' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card
            bordered={false}
            style={{
              borderRadius: 'var(--card-radius)',
              boxShadow: 'var(--card-shadow)',
            }}
            bodyStyle={{ padding: 'var(--card-padding)' }}
          >
            <Statistic
              title={
                <span style={{ color: 'var(--color-text-secondary)', fontSize: 'var(--stat-title-size)', fontWeight: 'var(--stat-title-weight)', fontFamily: 'var(--button-font-family)' }}>
                  Active Users
                </span>
              }
              value={employees.filter((e) => e.isActive).length}
              prefix={<UserOutlined style={{ color: 'var(--icon-color-green)', fontSize: 'var(--icon-size-lg)' }} />}
              valueStyle={{ color: 'var(--color-text-primary)', fontSize: 'var(--stat-value-size)', fontWeight: 'var(--stat-value-weight)', fontFamily: 'var(--button-font-family)', letterSpacing: 'var(--stat-value-spacing)' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card
            bordered={false}
            style={{
              borderRadius: 'var(--card-radius)',
              boxShadow: 'var(--card-shadow)',
            }}
            bodyStyle={{ padding: 'var(--card-padding)' }}
          >
            <Statistic
              title={
                <span style={{ color: 'var(--color-text-secondary)', fontSize: 'var(--stat-title-size)', fontWeight: 'var(--stat-title-weight)', fontFamily: 'var(--button-font-family)' }}>
                  Total Payroll
                </span>
              }
              value={125000}
              prefix={<DollarOutlined style={{ color: 'var(--icon-color-purple)', fontSize: 'var(--icon-size-lg)' }} />}
              valueStyle={{ color: 'var(--color-text-primary)', fontSize: 'var(--stat-value-size)', fontWeight: 'var(--stat-value-weight)', fontFamily: 'var(--button-font-family)', letterSpacing: 'var(--stat-value-spacing)' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card
            bordered={false}
            style={{
              borderRadius: 'var(--card-radius)',
              boxShadow: 'var(--card-shadow)',
            }}
            bodyStyle={{ padding: 'var(--card-padding)' }}
          >
            <Statistic
              title={
                <span style={{ color: 'var(--color-text-secondary)', fontSize: 'var(--stat-title-size)', fontWeight: 'var(--stat-title-weight)', fontFamily: 'var(--button-font-family)' }}>
                  Growth Rate
                </span>
              }
              value={12.5}
              suffix="%"
              prefix={<RiseOutlined style={{ color: 'var(--icon-color-cyan)', fontSize: 'var(--icon-size-lg)' }} />}
              valueStyle={{ color: 'var(--color-text-primary)', fontSize: 'var(--stat-value-size)', fontWeight: 'var(--stat-value-weight)', fontFamily: 'var(--button-font-family)', letterSpacing: 'var(--stat-value-spacing)' }}
            />
          </Card>
        </Col>
      </Row>

      {/* Employee Management Table */}
      <Card
        title={
          <span
            style={{
              fontSize: 'var(--card-title-size)',
              fontWeight: 'var(--card-title-weight)',
              color: 'var(--color-text-primary)',
              fontFamily: 'var(--button-font-family)',
              letterSpacing: 'var(--card-title-spacing)',
            }}
          >
            Employee Management
          </span>
        }
        bordered={false}
        style={{
          borderRadius: 'var(--card-radius-xl)',
          boxShadow: 'var(--card-shadow)',
        }}
        bodyStyle={{ padding: 'var(--table-card-padding-top) var(--table-card-padding-horizontal) var(--table-card-padding-bottom)' }}
        extra={
          <Space size="middle">
            <Button
              type="primary"
              icon={<UserOutlined />}
              style={{
                borderRadius: 'var(--button-radius)',
                height: 'var(--button-height)',
                border: 'none',
                background: 'var(--gradient-success)',
                fontFamily: 'var(--button-font-family)',
                fontWeight: 'var(--button-font-weight)',
                boxShadow: 'var(--button-shadow-success)',
                transition: 'all 250ms ease-in-out',
              }}
            >
              Add Employee
            </Button>
            <Button
              type="primary"
              icon={<CheckCircleOutlined />}
              style={{
                borderRadius: 'var(--button-radius)',
                height: 'var(--button-height)',
                border: 'none',
                background: 'var(--gradient-success)',
                fontFamily: 'var(--button-font-family)',
                fontWeight: 'var(--button-font-weight)',
                boxShadow: 'var(--button-shadow-success)',
                transition: 'all 250ms ease-in-out',
              }}
            >
              Approve All
            </Button>
          </Space>
        }
      >
        <Table
          columns={employeeColumns}
          dataSource={employees}
          rowKey="id"
          loading={loading}
          pagination={{
            pageSize: 10,
            showSizeChanger: false,
            showTotal: (total) => (
              <span
                style={{
                  color: 'var(--color-text-secondary)',
                  fontFamily: 'var(--button-font-family)',
                  fontWeight: 'var(--pagination-font-weight)',
                  fontSize: 'var(--pagination-font-size)',
                }}
              >
                Total {total} employees
              </span>
            ),
            style: { marginTop: 24 },
          }}
          size="middle"
          style={{ marginTop: 16 }}
        />
      </Card>
    </div>
  );
}

