import { useEffect, useState } from 'react';
import { Card, Row, Col, Statistic, Table, Tag, Button, Space, Progress } from 'antd';
import {
  CalendarOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  TrophyOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import PageHeader from '../components/PageHeader';
import api from '../services/api';

interface LeaveBalance {
  type: string;
  total: number;
  used: number;
  remaining: number;
}

interface MyLeaveRequest {
  id: string;
  leaveType: string;
  startDate: string;
  endDate: string;
  status: string;
  reason: string;
}

export default function EmployeeDashboard() {
  const [myLeaves, setMyLeaves] = useState<MyLeaveRequest[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchMyLeaves();
  }, []);

  const fetchMyLeaves = async () => {
    try {
      // This would filter by current user in real implementation
      const response = await api.get('/leaverequests');
      setMyLeaves(response.data);
    } catch (error) {
      console.error('Failed to fetch leave requests:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleClockIn = () => {
    console.log('Clock In');
  };

  const myLeaveColumns: ColumnsType<MyLeaveRequest> = [
    {
      title: 'Leave Type',
      dataIndex: 'leaveType',
      key: 'leaveType',
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
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => {
        const color =
          status === 'Approved'
            ? 'green'
            : status === 'Rejected'
            ? 'red'
            : 'orange';
        return <Tag color={color}>{status}</Tag>;
      },
    },
    {
      title: 'Reason',
      dataIndex: 'reason',
      key: 'reason',
      ellipsis: true,
    },
  ];

  const leaveBalances: LeaveBalance[] = [
    { type: 'Annual Leave', total: 20, used: 8, remaining: 12 },
    { type: 'Sick Leave', total: 10, used: 2, remaining: 8 },
    { type: 'Personal Leave', total: 5, used: 1, remaining: 4 },
  ];

  return (
    <div>
      {/* Page Header */}
      <PageHeader title="Employee Dashboard" subtitle="My Workspace" />

      {/* Employee Stats */}
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
                  Days Present
                </span>
              }
              value={22}
              suffix="/ 23"
              prefix={<CheckCircleOutlined style={{ color: 'var(--icon-color-green)', fontSize: 'var(--icon-size-lg)' }} />}
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
                  Leaves Taken
                </span>
              }
              value={11}
              suffix="/ 35"
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
              title={
                <span style={{ color: 'var(--color-text-secondary)', fontSize: 'var(--stat-title-size)', fontWeight: 'var(--stat-title-weight)', fontFamily: 'var(--button-font-family)' }}>
                  Hours This Month
                </span>
              }
              value={176}
              prefix={<ClockCircleOutlined style={{ color: 'var(--icon-color-cyan)', fontSize: 'var(--icon-size-lg)' }} />}
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
                  Performance Score
                </span>
              }
              value={92}
              suffix="/ 100"
              prefix={<TrophyOutlined style={{ color: 'var(--icon-color-purple)', fontSize: 'var(--icon-size-lg)' }} />}
              valueStyle={{ color: 'var(--color-text-primary)', fontSize: 'var(--stat-value-size)', fontWeight: 'var(--stat-value-weight)', fontFamily: 'var(--button-font-family)', letterSpacing: 'var(--stat-value-spacing)' }}
            />
          </Card>
        </Col>
      </Row>

      {/* Leave Balance Overview */}
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
            Leave Balance
          </span>
        }
        bordered={false}
        style={{
          borderRadius: 20,
          boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)',
          marginBottom: 24,
        }}
        bodyStyle={{ padding: '24px 32px 32px' }}
      >
        <Row gutter={[24, 24]}>
          {leaveBalances.map((balance) => (
            <Col xs={24} md={8} key={balance.type}>
              <div style={{ padding: '16px 0' }}>
                <div
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    marginBottom: 8,
                  }}
                >
                  <span
                    style={{
                      fontSize: 14,
                      fontWeight: 600,
                      color: 'var(--color-text-primary)',
                      fontFamily: 'Inter, sans-serif',
                    }}
                  >
                    {balance.type}
                  </span>
                  <span
                    style={{
                      fontSize: 14,
                      fontWeight: 600,
                      color: 'var(--color-text-secondary)',
                      fontFamily: 'Inter, sans-serif',
                    }}
                  >
                    {balance.remaining} / {balance.total}
                  </span>
                </div>
                <Progress
                  percent={Math.round((balance.remaining / balance.total) * 100)}
                  strokeColor="#14B8A6"
                  trailColor="#E2E8F0"
                  showInfo={false}
                />
                <div
                  style={{
                    marginTop: 8,
                    fontSize: 'var(--font-size-sm)',
                    color: 'var(--color-text-secondary)',
                    fontFamily: 'var(--button-font-family)',
                  }}
                >
                  {balance.used} days used
                </div>
              </div>
            </Col>
          ))}
        </Row>
      </Card>

      {/* My Leave Requests */}
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
            My Leave Requests
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
              icon={<CalendarOutlined />}
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
              Request Leave
            </Button>
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
          </Space>
        }
      >
        <Table
          columns={myLeaveColumns}
          dataSource={myLeaves}
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
                Total {total} requests
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

