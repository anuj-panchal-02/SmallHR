import { useState } from 'react';
import { Card, Row, Col, Tag, Button, Select, Avatar, Progress } from 'antd';
import {
  UserOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  FileTextOutlined,
  UpOutlined,
  DownOutlined,
  PlusOutlined,
  BellOutlined,
  TrophyOutlined,
  RocketOutlined,
  BarChartOutlined,
  AuditOutlined,
} from '@ant-design/icons';
import PageHeader from '../components/PageHeader';

export default function HRDashboard() {
  const [expandedItems, setExpandedItems] = useState<string[]>(['1']);
  const [selectedTimeframe, setSelectedTimeframe] = useState('Week');

  const toggleExpanded = (id: string) => {
    setExpandedItems(prev => 
      prev.includes(id) 
        ? prev.filter(item => item !== id)
        : [...prev, id]
    );
  };

  // Mock attendance data for weekly chart
  const weeklyData = [
    { day: 'S', value: 45 },
    { day: 'M', value: 48 },
    { day: 'T', value: 52 },
    { day: 'W', value: 50 },
    { day: 'T', value: 49 },
    { day: 'F', value: 47 },
    { day: 'S', value: 40 },
  ];

  const maxValue = Math.max(...weeklyData.map(d => d.value));
  const selectedDay = 'T';

  // Mock recent employees data
  const recentEmployees = [
    { id: '1', name: 'Alice Johnson', role: 'Senior Developer', avatar: null, status: 'Active' },
    { id: '2', name: 'Bob Smith', role: 'UX Designer', avatar: null, status: 'Active' },
    { id: '3', name: 'Carol White', role: 'Product Manager', avatar: null, status: 'Active' },
  ];

  // Sample leave requests data
  const sampleLeaveRequests = [
    {
      id: '1',
      title: 'Annual Leave Request',
      employee: 'John Doe',
      dates: 'Dec 15 - Dec 18',
      days: 4,
      status: 'Pending',
      description: 'Personal vacation to spend time with family during the holidays.',
    },
    {
      id: '2',
      title: 'Sick Leave',
      employee: 'Jane Smith',
      dates: 'Dec 12',
      days: 1,
      status: 'Approved',
      description: 'Doctor\'s appointment for annual checkup.',
    },
    {
      id: '3',
      title: 'Emergency Leave',
      employee: 'Bob Wilson',
      dates: 'Dec 20',
      days: 1,
      status: 'Pending',
      description: 'Family emergency requiring immediate attention.',
    },
  ];

  return (
    <div>
      {/* Page Header */}
      <PageHeader title="HR Dashboard" subtitle="Leave & Attendance Management" />

      {/* Modern Dashboard Layout */}
      <Row gutter={[16, 16]}>
        {/* Left Column */}
        <Col xs={24} md={14}>
          {/* Attendance Tracker Card */}
          <Card
            bordered={false}
            style={{
              marginBottom: 16,
              borderRadius: 'var(--card-radius)',
              boxShadow: 'var(--card-shadow)',
            }}
            bodyStyle={{ padding: 'var(--card-padding)' }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4 }}>
                  <BarChartOutlined style={{ fontSize: 16, color: '#4f46e5' }} />
                  <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600, color: '#1e293b' }}>
                    Attendance Tracker
                  </h3>
                </div>
                <p style={{ margin: 0, fontSize: 13, color: '#64748b' }}>
                  Track employee attendance over time and access detailed data on each department.
                </p>
              </div>
              <Select
                value={selectedTimeframe}
                onChange={setSelectedTimeframe}
                style={{ width: 100 }}
                bordered={false}
                options={[
                  { label: 'Week', value: 'Week' },
                  { label: 'Month', value: 'Month' },
                  { label: 'Year', value: 'Year' },
                ]}
              />
            </div>

            {/* Weekly Chart */}
            <div style={{ marginBottom: 16 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', height: 200 }}>
                {weeklyData.map((data, index) => {
                  const height = (data.value / maxValue) * 180;
                  const isSelected = data.day === selectedDay;
                  return (
                    <div key={index} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                      <div
                        style={{
                          width: '100%',
                          maxWidth: 40,
                          height: height,
                          background: isSelected 
                            ? 'linear-gradient(180deg, #4f46e5 0%, #818cf8 100%)'
                            : 'linear-gradient(180deg, #e0e7ff 0%, #c7d2fe 100%)',
                          borderRadius: '8px 8px 0 0',
                          marginBottom: 8,
                          position: 'relative',
                          transition: 'all 0.3s',
                        }}
                      >
                        {isSelected && (
                          <div
                            style={{
                              position: 'absolute',
                              top: -40,
                              left: '50%',
                              transform: 'translateX(-50%)',
                              background: '#1e293b',
                              color: 'white',
                              padding: '6px 12px',
                              borderRadius: 8,
                              fontSize: 12,
                              fontWeight: 600,
                              whiteSpace: 'nowrap',
                            }}
                          >
                            {data.value} employees
                          </div>
                        )}
                      </div>
                      <button
                        onClick={() => console.log('Day selected:', data.day)}
                        style={{
                          width: 36,
                          height: 36,
                          borderRadius: '50%',
                          border: 'none',
                          background: isSelected ? '#1e293b' : '#f1f5f9',
                          color: isSelected ? 'white' : '#64748b',
                          fontSize: 14,
                          fontWeight: 600,
                          cursor: 'pointer',
                          transition: 'all 0.3s',
                        }}
                      >
                        {data.day}
                      </button>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* Summary */}
            <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
              <div style={{ fontSize: 24, fontWeight: 700, color: '#16a34a' }}>+12%</div>
              <div style={{ fontSize: 14, color: '#64748b' }}>
                This week's attendance is higher than last week.
              </div>
            </div>
          </Card>

          {/* Recent Leave Requests */}
          <Card
            bordered={false}
            style={{
              marginBottom: 16,
              borderRadius: 'var(--card-radius)',
              boxShadow: 'var(--card-shadow)',
            }}
            bodyStyle={{ padding: 'var(--card-padding)' }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
              <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600, color: '#1e293b' }}>
                Recent Leave Requests
              </h3>
              <Button type="link" style={{ padding: 0, fontWeight: 500 }}>
                See All Requests
              </Button>
            </div>

            <div>
              {sampleLeaveRequests.map((request, index) => {
                const isExpanded = expandedItems.includes(request.id);
                const statusColor = request.status === 'Approved' ? '#16a34a' : request.status === 'Pending' ? '#f59e0b' : '#ef4444';
                
                return (
                  <div key={request.id}>
                    <div
                      style={{
                        padding: '16px 0',
                        borderBottom: index !== sampleLeaveRequests.length - 1 ? '1px solid #e2e8f0' : 'none',
                      }}
                    >
                      <div style={{ display: 'flex', alignItems: 'flex-start', gap: 16 }}>
                        <div
                          style={{
                            width: 48,
                            height: 48,
                            borderRadius: 12,
                            background: request.status === 'Approved' ? '#dcfce7' : request.status === 'Pending' ? '#fef3c7' : '#fee2e2',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            fontSize: 16,
                          }}
                        >
                          {request.status === 'Approved' ? (
                            <CheckCircleOutlined style={{ color: statusColor }} />
                          ) : request.status === 'Pending' ? (
                            <ClockCircleOutlined style={{ color: statusColor }} />
                          ) : (
                            <FileTextOutlined style={{ color: statusColor }} />
                          )}
                        </div>
                        <div style={{ flex: 1 }}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                            <div>
                              <div style={{ fontSize: 16, fontWeight: 600, color: '#1e293b', marginBottom: 4 }}>
                                {request.title}
                              </div>
                              <div style={{ fontSize: 14, color: '#64748b' }}>
                                {request.employee}
                              </div>
                            </div>
                            <Tag
                              color={request.status === 'Approved' ? 'green' : request.status === 'Pending' ? 'orange' : 'red'}
                              style={{ 
                                borderRadius: 12, 
                                padding: '4px 12px', 
                                fontSize: 12, 
                                fontWeight: 500,
                                border: 'none'
                              }}
                            >
                              {request.status}
                            </Tag>
                          </div>
                          <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginTop: 8, fontSize: 14, color: '#64748b' }}>
                            <span>{request.days} days</span>
                            <span>•</span>
                            <span>{request.dates}</span>
                            <Button
                              type="link"
                              size="small"
                              onClick={() => toggleExpanded(request.id)}
                              icon={isExpanded ? <UpOutlined /> : <DownOutlined />}
                              style={{ marginLeft: 'auto', padding: 0, height: 'auto' }}
                            />
                          </div>
                          {isExpanded && (
                            <div style={{ marginTop: 16, padding: 16, background: '#f8fafc', borderRadius: 12 }}>
                              <p style={{ margin: 0, fontSize: 14, color: '#475569' }}>
                                {request.description}
                              </p>
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </Card>

          {/* Team Highlights Card */}
          <Card
            bordered={false}
            style={{
              marginBottom: 16,
              borderRadius: 'var(--card-radius)',
              boxShadow: 'var(--card-shadow)',
            }}
            bodyStyle={{ padding: 'var(--card-padding)' }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
              <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600, color: '#1e293b' }}>
                Team Highlights
              </h3>
              <Button type="link" style={{ padding: 0, fontWeight: 500 }}>
                See All
              </Button>
            </div>

            <div>
              {recentEmployees.map((emp, index) => (
                <div
                  key={emp.id}
                  style={{
                    padding: '16px 0',
                    borderBottom: index !== recentEmployees.length - 1 ? '1px solid #e2e8f0' : 'none',
                    display: 'flex',
                    alignItems: 'center',
                    gap: 16,
                  }}
                >
                  <Avatar
                    size={40}
                    icon={<UserOutlined />}
                    style={{ background: '#4f46e5' }}
                  />
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: 15, fontWeight: 500, color: '#1e293b', marginBottom: 4 }}>
                      {emp.name}
                    </div>
                    <div style={{ fontSize: 13, color: '#64748b' }}>
                      {emp.role}
                    </div>
                  </div>
                  <Button
                    type="primary"
                    shape="circle"
                    icon={<PlusOutlined />}
                    size="small"
                    style={{
                      background: '#4f46e5',
                      border: 'none',
                      width: 32,
                      height: 32,
                    }}
                  />
                </div>
              ))}
            </div>
          </Card>
        </Col>

        {/* Right Column */}
        <Col xs={24} md={10}>
          {/* Attendance Progress Card */}
          <Card
            bordered={false}
            style={{
              marginBottom: 16,
              borderRadius: 'var(--card-radius)',
              boxShadow: 'var(--card-shadow)',
            }}
            bodyStyle={{ padding: 'var(--card-padding)' }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 }}>
              <AuditOutlined style={{ fontSize: 16, color: '#4f46e5' }} />
              <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600, color: '#1e293b' }}>
                Attendance Progress
              </h3>
            </div>

            <div>
              <div style={{ marginBottom: 12 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                  <span style={{ fontSize: 13, color: '#64748b' }}>Present Today</span>
                  <span style={{ fontSize: 20, fontWeight: 700, color: '#1e293b' }}>45</span>
                </div>
                <Progress
                  percent={90}
                  strokeColor="#4f46e5"
                  trailColor="#e0e7ff"
                  showInfo={false}
                  style={{ height: 8 }}
                />
                <div style={{ fontSize: 12, color: '#94a3b8', marginTop: 4 }}>of 50 employees</div>
              </div>

              <div style={{ marginBottom: 12, paddingTop: 24, borderTop: '1px solid #e2e8f0' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                  <span style={{ fontSize: 13, color: '#64748b' }}>On Leave</span>
                  <span style={{ fontSize: 20, fontWeight: 700, color: '#1e293b' }}>5</span>
                </div>
                <Progress
                  percent={10}
                  strokeColor="#f59e0b"
                  trailColor="#fef3c7"
                  showInfo={false}
                  style={{ height: 8 }}
                />
              </div>

              <div style={{ paddingTop: 24, borderTop: '1px solid #e2e8f0' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                  <span style={{ fontSize: 13, color: '#64748b' }}>Absent</span>
                  <span style={{ fontSize: 20, fontWeight: 700, color: '#1e293b' }}>0</span>
                </div>
                <Progress
                  percent={0}
                  strokeColor="#ef4444"
                  trailColor="#fee2e2"
                  showInfo={false}
                  style={{ height: 8 }}
                />
              </div>
            </div>
          </Card>

          {/* Premium Features Card */}
          <Card
            bordered={false}
            style={{
              marginBottom: 16,
              borderRadius: 'var(--card-radius)',
              background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
              boxShadow: '0 4px 12px rgba(102, 126, 234, 0.3)',
            }}
            bodyStyle={{ padding: 'var(--card-padding-lg)', color: 'white' }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
              <RocketOutlined style={{ fontSize: 20 }} />
              <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600 }}>
                Unlock Premium Features
              </h3>
            </div>
            <p style={{ marginBottom: 12, fontSize: 14, opacity: 0.9 }}>
              Get access to exclusive benefits and expand your HR management capabilities.
            </p>
            <Button
              type="primary"
              style={{
                background: 'white',
                color: '#667eea',
                border: 'none',
                borderRadius: 8,
                height: 42,
                fontSize: 14,
                fontWeight: 600,
                display: 'flex',
                alignItems: 'center',
                gap: 8,
              }}
              icon={<TrophyOutlined />}
            >
              Upgrade Now
            </Button>
          </Card>

          {/* Quick Stats Card */}
          <Card
            bordered={false}
            style={{
              marginBottom: 16,
              borderRadius: 'var(--card-radius)',
              boxShadow: 'var(--card-shadow)',
            }}
            bodyStyle={{ padding: 'var(--card-padding)' }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 }}>
              <BellOutlined style={{ fontSize: 16, color: '#4f46e5' }} />
              <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600, color: '#1e293b' }}>
                Notifications
              </h3>
            </div>

            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 12, padding: '12px 0' }}>
                <div style={{ width: 40, height: 40, borderRadius: '50%', background: '#fef3c7', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <BellOutlined style={{ color: '#f59e0b', fontSize: 16 }} />
                </div>
                <div style={{ flex: 1 }}>
                  <div style={{ fontSize: 14, fontWeight: 500, color: '#1e293b' }}>
                    New Leave Request
                  </div>
                  <div style={{ fontSize: 12, color: '#64748b' }}>
                    2 minutes ago
                  </div>
                </div>
                <div style={{ width: 8, height: 8, borderRadius: '50%', background: '#ef4444' }} />
              </div>
            </div>
      </Card>
        </Col>
      </Row>
    </div>
  );
}
