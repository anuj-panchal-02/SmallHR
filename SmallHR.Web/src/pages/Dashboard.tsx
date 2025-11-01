import { useState, useEffect } from 'react';
import { Card, Row, Col, Statistic, Button, Table, Tag, Space, message } from 'antd';
import { 
  UserOutlined, 
  ClockCircleOutlined, 
  CalendarOutlined, 
  CheckCircleOutlined
} from '@ant-design/icons';
import { employeeAPI, leaveRequestAPI, attendanceAPI } from '../services/api';
import PageHeader from '../components/PageHeader';
import type { Employee, LeaveRequest } from '../types/api';

export default function Dashboard() {
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [leaveRequests, setLeaveRequests] = useState<LeaveRequest[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchDashboardData();
  }, []);

  const fetchDashboardData = async () => {
    try {
      const [employeesRes, leaveRequestsRes] = await Promise.all([
        employeeAPI.getActive(),
        leaveRequestAPI.getPending(),
      ]);
      
      setEmployees(employeesRes.data);
      setLeaveRequests(leaveRequestsRes.data);
    } catch (error: any) {
      message.error('Failed to load dashboard data');
    } finally {
      setLoading(false);
    }
  };

  const handleClockIn = async () => {
    try {
      // Assuming the logged-in user has an employee record
      await attendanceAPI.clockIn({
        employeeId: 1, // You would get this from the user's employee record
        clockInTime: new Date().toISOString(),
      });
      message.success('Clocked in successfully!');
    } catch (error: any) {
      message.error('Failed to clock in');
    }
  };

  const handleClockOut = async () => {
    try {
      await attendanceAPI.clockOut({
        employeeId: 1,
        clockOutTime: new Date().toISOString(),
      });
      message.success('Clocked out successfully!');
    } catch (error: any) {
      message.error('Failed to clock out');
    }
  };

  const leaveRequestColumns = [
    {
      title: 'Employee',
      dataIndex: 'employeeName',
      key: 'employeeName',
    },
    {
      title: 'Leave Type',
      dataIndex: 'leaveType',
      key: 'leaveType',
      render: (type: string) => <Tag color="blue">{type}</Tag>,
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
      render: (date: string) => new Date(date).toLocaleDateString(),
    },
    {
      title: 'Days',
      dataIndex: 'totalDays',
      key: 'totalDays',
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => {
        const color = status === 'Pending' ? 'orange' : status === 'Approved' ? 'green' : 'red';
        return <Tag color={color}>{status}</Tag>;
      },
    },
  ];

  return (
    <div>
      {/* Page Header */}
      <PageHeader 
        title="Management Department"
        subtitle="HR Dashboard"
      />

      {/* Quick Stats - FUJIN Style */}
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
              title={<span style={{ color: 'var(--color-text-secondary)', fontSize: 'var(--stat-title-size)', fontWeight: 'var(--stat-title-weight)', fontFamily: 'var(--button-font-family)' }}>Total Employees</span>}
              value={employees.length}
              prefix={<UserOutlined style={{ color: 'var(--icon-color-teal)', fontSize: 'var(--icon-size-lg)' }} />}
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
              title={<span style={{ color: 'var(--color-text-secondary)', fontSize: 'var(--stat-title-size)', fontWeight: 'var(--stat-title-weight)', fontFamily: 'var(--button-font-family)' }}>Pending Leaves</span>}
              value={leaveRequests.length}
              prefix={<CalendarOutlined style={{ color: 'var(--icon-color-orange)', fontSize: 'var(--icon-size-lg)' }} />}
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
              title={<span style={{ color: 'var(--color-text-secondary)', fontSize: 'var(--stat-title-size)', fontWeight: 'var(--stat-title-weight)', fontFamily: 'var(--button-font-family)' }}>Today's Attendance</span>}
              value={employees.length}
              prefix={<CheckCircleOutlined style={{ color: 'var(--icon-color-cyan)', fontSize: 'var(--icon-size-lg)' }} />}
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
              title={<span style={{ color: 'var(--color-text-secondary)', fontSize: 'var(--stat-title-size)', fontWeight: 'var(--stat-title-weight)', fontFamily: 'var(--button-font-family)' }}>Active Employees</span>}
              value={employees.filter(e => e.isActive).length}
              prefix={<UserOutlined style={{ color: 'var(--icon-color-purple)', fontSize: 20 }} />}
              valueStyle={{ color: 'var(--color-text-primary)', fontSize: 'var(--stat-value-size)', fontWeight: 'var(--stat-value-weight)', fontFamily: 'var(--button-font-family)', letterSpacing: 'var(--stat-value-spacing)' }}
            />
          </Card>
        </Col>
      </Row>

      {/* Pending Leave Requests - FUJIN Table */}
      <Card 
        title={
          <span style={{ 
            fontSize: 'var(--card-title-size)', 
            fontWeight: 'var(--card-title-weight)', 
            color: 'var(--color-text-primary)',
            fontFamily: 'var(--button-font-family)',
            letterSpacing: 'var(--card-title-spacing)'
          }}>
            Pending Leave Requests
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
              icon={<ClockCircleOutlined />}
              onClick={handleClockIn}
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
              Clock In
            </Button>
            <Button 
              icon={<ClockCircleOutlined />}
              onClick={handleClockOut}
              style={{ 
                borderRadius: 'var(--button-radius)', 
                height: 'var(--button-height)',
                borderColor: 'var(--glass-border)',
                color: 'var(--color-text-secondary)',
                transition: 'all 250ms ease-in-out',
                fontWeight: 'var(--button-font-weight)',
                fontFamily: 'var(--button-font-family)',
              }}
            >
              Clock Out
            </Button>
            <Button 
              icon={<CalendarOutlined />}
              style={{ 
                borderRadius: 'var(--button-radius)', 
                height: 'var(--button-height)',
                borderColor: 'var(--glass-border)',
                color: 'var(--color-text-secondary)',
                transition: 'all 250ms ease-in-out',
                fontWeight: 'var(--button-font-weight)',
                fontFamily: 'var(--button-font-family)',
              }}
            >
              Request Leave
            </Button>
          </Space>
        }
      >
        <Table
          columns={leaveRequestColumns}
          dataSource={leaveRequests}
          rowKey="id"
          loading={loading}
          pagination={{ 
            pageSize: 10,
            showSizeChanger: false,
            showTotal: (total) => (
              <span style={{ 
                color: 'var(--color-text-secondary)', 
                fontFamily: 'var(--button-font-family)',
                fontWeight: 'var(--pagination-font-weight)',
                fontSize: 'var(--pagination-font-size)'
              }}>
                Total {total} requests
              </span>
            ),
            style: { marginTop: 24 }
          }}
          size="middle"
          style={{ marginTop: 16 }}
        />
      </Card>
    </div>
  );
}

