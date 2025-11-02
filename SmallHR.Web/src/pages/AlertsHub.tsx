import { useState, useEffect } from 'react';
import { Card, Table, Tag, Space, Row, Col, Statistic, Button, Select, Alert } from 'antd';
import {
  WarningOutlined,
  DollarOutlined,
  ExclamationCircleOutlined,
  CheckCircleOutlined,
  ReloadOutlined,
  BellOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import PageHeader from '../components/PageHeader';
import api from '../services/api';
import { useNotification } from '../contexts/NotificationContext';

interface AlertItem {
  id: number;
  tenantId: number;
  tenantName: string;
  alertType: 'PaymentFailure' | 'Overage' | 'Error' | 'Suspension';
  severity: 'High' | 'Medium' | 'Low';
  message: string;
  status: 'Active' | 'Resolved' | 'Acknowledged';
  createdAt: string;
  resolvedAt: string | null;
  metadata: {
    subscriptionId?: number;
    planName?: string;
    amount?: number;
    limit?: number;
    usage?: number;
    errorCode?: string;
  } | null;
}

export default function AlertsHub() {
  const notify = useNotification();
  const [alerts, setAlerts] = useState<AlertItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [statusFilter, setStatusFilter] = useState<string | undefined>();
  const [typeFilter, setTypeFilter] = useState<string | undefined>();
  const [severityFilter, setSeverityFilter] = useState<string | undefined>();

  useEffect(() => {
    fetchAlerts();
  }, [statusFilter, typeFilter, severityFilter]);

  const fetchAlerts = async () => {
    setLoading(true);
    try {
      // Note: You'll need to create an alerts endpoint
      // For now, we'll fetch from tenants and subscriptions
      const tenantsResponse = await api.get('/admin/tenants', {
        params: { status: 'Suspended', pageSize: 100 },
      });

      const subscriptionsResponse = await api.get('/admin/subscriptions', {
        params: { status: 'PastDue', pageSize: 100 },
      });

      // Build alerts from data
      const alertsData: AlertItem[] = [];

      // Payment failures
      subscriptionsResponse.data?.subscriptions?.forEach((sub: any) => {
        if (sub.status === 'PastDue') {
          alertsData.push({
            id: alertsData.length + 1,
            tenantId: sub.tenantId,
            tenantName: sub.tenant?.name || 'Unknown',
            alertType: 'PaymentFailure',
            severity: 'High',
            message: `Payment failed for tenant ${sub.tenant?.name || sub.tenantId}. Subscription is past due.`,
            status: 'Active',
            createdAt: sub.updatedAt || sub.createdAt,
            resolvedAt: null,
            metadata: {
              subscriptionId: sub.id,
              planName: sub.planName,
              amount: sub.price,
            },
          });
        }
      });

      // Suspended tenants
      tenantsResponse.data?.tenants?.forEach((tenant: any) => {
        if (tenant.status === 'Suspended') {
          alertsData.push({
            id: alertsData.length + 1,
            tenantId: tenant.id,
            tenantName: tenant.name,
            alertType: 'Suspension',
            severity: 'High',
            message: `Tenant ${tenant.name} is suspended.`,
            status: 'Active',
            createdAt: tenant.updatedAt || tenant.createdAt,
            resolvedAt: null,
            metadata: null,
          });
        }
      });

      // Filter alerts
      let filtered = alertsData;
      if (statusFilter) {
        filtered = filtered.filter(a => a.status === statusFilter);
      }
      if (typeFilter) {
        filtered = filtered.filter(a => a.alertType === typeFilter);
      }
      if (severityFilter) {
        filtered = filtered.filter(a => a.severity === severityFilter);
      }

      setAlerts(filtered);
    } catch (error: any) {
      notify.error('Failed to Load Alerts', 'Unable to fetch alerts.');
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const handleAcknowledge = async (alertId: number) => {
    try {
      // Placeholder for acknowledge endpoint
      await api.post(`/admin/alerts/${alertId}/acknowledge`);
      notify.success('Alert Acknowledged', 'Alert has been marked as acknowledged.');
      fetchAlerts();
    } catch (error: any) {
      notify.error('Failed to Acknowledge', 'Unable to acknowledge alert.');
    }
  };

  const handleResolve = async (alertId: number) => {
    try {
      // Placeholder for resolve endpoint
      await api.post(`/admin/alerts/${alertId}/resolve`);
      notify.success('Alert Resolved', 'Alert has been resolved.');
      fetchAlerts();
    } catch (error: any) {
      notify.error('Failed to Resolve', 'Unable to resolve alert.');
    }
  };

  const getAlertTypeTag = (type: string) => {
    const typeConfig: Record<string, { color: string; icon: any }> = {
      PaymentFailure: { color: 'red', icon: <DollarOutlined /> },
      Overage: { color: 'orange', icon: <ExclamationCircleOutlined /> },
      Error: { color: 'red', icon: <WarningOutlined /> },
      Suspension: { color: 'red', icon: <WarningOutlined /> },
    };
    const config = typeConfig[type] || { color: 'default', icon: null };
    return (
      <Tag color={config.color} icon={config.icon}>
        {type}
      </Tag>
    );
  };

  const getSeverityTag = (severity: string) => {
    const severityConfig: Record<string, { color: string }> = {
      High: { color: 'red' },
      Medium: { color: 'orange' },
      Low: { color: 'blue' },
    };
    const config = severityConfig[severity] || { color: 'default' };
    return <Tag color={config.color}>{severity}</Tag>;
  };

  const columns: ColumnsType<AlertItem> = [
    {
      title: 'Date',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => new Date(date).toLocaleString(),
      sorter: (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
    },
    {
      title: 'Type',
      dataIndex: 'alertType',
      key: 'alertType',
      render: (type: string) => getAlertTypeTag(type),
      filters: [
        { text: 'Payment Failure', value: 'PaymentFailure' },
        { text: 'Overage', value: 'Overage' },
        { text: 'Error', value: 'Error' },
        { text: 'Suspension', value: 'Suspension' },
      ],
    },
    {
      title: 'Severity',
      dataIndex: 'severity',
      key: 'severity',
      render: (severity: string) => getSeverityTag(severity),
      filters: [
        { text: 'High', value: 'High' },
        { text: 'Medium', value: 'Medium' },
        { text: 'Low', value: 'Low' },
      ],
    },
    {
      title: 'Tenant',
      key: 'tenant',
      render: (_: any, record: AlertItem) => (
        <Button
          type="link"
          onClick={() => window.location.href = `/admin/tenants/${record.tenantId}`}
        >
          {record.tenantName}
        </Button>
      ),
    },
    {
      title: 'Message',
      dataIndex: 'message',
      key: 'message',
      ellipsis: true,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => (
        <Tag color={status === 'Active' ? 'red' : status === 'Resolved' ? 'green' : 'blue'}>
          {status}
        </Tag>
      ),
      filters: [
        { text: 'Active', value: 'Active' },
        { text: 'Resolved', value: 'Resolved' },
        { text: 'Acknowledged', value: 'Acknowledged' },
      ],
    },
    {
      title: 'Actions',
      key: 'actions',
      width: 200,
      render: (_: any, record: AlertItem) => (
        <Space>
          {record.status === 'Active' && (
            <>
              <Button size="small" onClick={() => handleAcknowledge(record.id)}>
                Acknowledge
              </Button>
              <Button size="small" type="primary" onClick={() => handleResolve(record.id)}>
                Resolve
              </Button>
            </>
          )}
        </Space>
      ),
    },
  ];

  const activeAlerts = alerts.filter(a => a.status === 'Active');
  const highSeverityAlerts = alerts.filter(a => a.severity === 'High' && a.status === 'Active');
  const paymentFailures = alerts.filter(a => a.alertType === 'PaymentFailure' && a.status === 'Active');

  return (
    <div>
      <PageHeader title="Alerts Hub" />

      {highSeverityAlerts.length > 0 && (
        <Alert
          message={`${highSeverityAlerts.length} High Severity Alert${highSeverityAlerts.length !== 1 ? 's' : ''} Require Attention`}
          type="error"
          icon={<WarningOutlined />}
          showIcon
          style={{ marginBottom: 16 }}
          action={
            <Button size="small" onClick={fetchAlerts}>
              Refresh
            </Button>
          }
        />
      )}

      <Row gutter={16} style={{ marginBottom: 16 }}>
        <Col span={6}>
          <Card>
            <Statistic
              title="Active Alerts"
              value={activeAlerts.length}
              prefix={<BellOutlined />}
              valueStyle={{ color: activeAlerts.length > 0 ? '#cf1322' : '#3f8600' }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="High Severity"
              value={highSeverityAlerts.length}
              prefix={<WarningOutlined />}
              valueStyle={{ color: '#cf1322' }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="Payment Failures"
              value={paymentFailures.length}
              prefix={<DollarOutlined />}
              valueStyle={{ color: '#cf1322' }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="Resolved"
              value={alerts.filter(a => a.status === 'Resolved').length}
              prefix={<CheckCircleOutlined />}
              valueStyle={{ color: '#3f8600' }}
            />
          </Card>
        </Col>
      </Row>

      <Card>
        <Row gutter={16} style={{ marginBottom: 16 }}>
          <Col span={6}>
            <Select
              placeholder="Status"
              style={{ width: '100%' }}
              allowClear
              value={statusFilter}
              onChange={setStatusFilter}
            >
              <Select.Option value="Active">Active</Select.Option>
              <Select.Option value="Resolved">Resolved</Select.Option>
              <Select.Option value="Acknowledged">Acknowledged</Select.Option>
            </Select>
          </Col>
          <Col span={6}>
            <Select
              placeholder="Type"
              style={{ width: '100%' }}
              allowClear
              value={typeFilter}
              onChange={setTypeFilter}
            >
              <Select.Option value="PaymentFailure">Payment Failure</Select.Option>
              <Select.Option value="Overage">Overage</Select.Option>
              <Select.Option value="Error">Error</Select.Option>
              <Select.Option value="Suspension">Suspension</Select.Option>
            </Select>
          </Col>
          <Col span={6}>
            <Select
              placeholder="Severity"
              style={{ width: '100%' }}
              allowClear
              value={severityFilter}
              onChange={setSeverityFilter}
            >
              <Select.Option value="High">High</Select.Option>
              <Select.Option value="Medium">Medium</Select.Option>
              <Select.Option value="Low">Low</Select.Option>
            </Select>
          </Col>
          <Col span={6}>
            <Button icon={<ReloadOutlined />} onClick={fetchAlerts} loading={loading}>
              Refresh
            </Button>
          </Col>
        </Row>

        <Table
          columns={columns}
          dataSource={alerts}
          rowKey="id"
          loading={loading}
          pagination={{ pageSize: 20, showTotal: (total) => `Total ${total} alerts` }}
        />
      </Card>
    </div>
  );
}

