import { useState, useEffect } from 'react';
import { Card, Table, Button, Tag, Space, Row, Col, Statistic, DatePicker, Select, Modal } from 'antd';
import {
  ReloadOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  FileTextOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import PageHeader from '../components/PageHeader';
import api from '../services/api';
import { useNotification } from '../contexts/NotificationContext';
import dayjs from 'dayjs';

interface WebhookEvent {
  id: number;
  eventType: string;
  provider: string;
  status: string;
  tenantId: number | null;
  subscriptionId: number | null;
  payload: string;
  processed: boolean;
  createdAt: string;
  error: string | null;
}

interface ReconciliationResult {
  reconciled: number;
  discrepancies: number;
  totalChecked: number;
}

export default function BillingCenter() {
  const notify = useNotification();
  const [webhooks, setWebhooks] = useState<WebhookEvent[]>([]);
  const [loading, setLoading] = useState(false);
  const [reconciling, setReconciling] = useState(false);
  const [startDate, setStartDate] = useState<dayjs.Dayjs | null>(dayjs().subtract(30, 'days'));
  const [endDate, setEndDate] = useState<dayjs.Dayjs | null>(dayjs());
  const [statusFilter, setStatusFilter] = useState<string | undefined>();
  const [providerFilter, setProviderFilter] = useState<string | undefined>();

  useEffect(() => {
    fetchWebhookEvents();
  }, [startDate, endDate, statusFilter, providerFilter]);

  const fetchWebhookEvents = async () => {
    setLoading(true);
    try {
      // Note: You'll need to create a webhook events endpoint
      // For now, this is a placeholder
      const response = await api.get('/admin/billing/webhooks', {
        params: {
          startDate: startDate?.toISOString(),
          endDate: endDate?.toISOString(),
          status: statusFilter,
          provider: providerFilter,
        },
      });
      setWebhooks(response.data.webhooks || []);
    } catch (error: any) {
      // If endpoint doesn't exist yet, show empty state
      console.log('Webhook events endpoint not implemented yet');
      setWebhooks([]);
    } finally {
      setLoading(false);
    }
  };

  const handleReconcile = async () => {
    setReconciling(true);
    try {
      // Placeholder for reconciliation endpoint
      const response = await api.post('/admin/billing/reconcile', {
        startDate: startDate?.toISOString(),
        endDate: endDate?.toISOString(),
      });
      
      const result: ReconciliationResult = response.data;
      notify.success(
        'Reconciliation Complete',
        `Reconciled ${result.reconciled} subscriptions. Found ${result.discrepancies} discrepancies.`
      );
      
      // Refresh webhook events
      fetchWebhookEvents();
    } catch (error: any) {
      notify.error('Reconciliation Failed', error.response?.data?.message || 'Unable to reconcile billing data.');
      console.error(error);
    } finally {
      setReconciling(false);
    }
  };

  const getStatusTag = (status: string) => {
    const statusConfig: Record<string, { color: string }> = {
      Success: { color: 'green' },
      Failed: { color: 'red' },
      Pending: { color: 'orange' },
      Processed: { color: 'blue' },
    };
    const config = statusConfig[status] || { color: 'default' };
    return <Tag color={config.color}>{status}</Tag>;
  };

  const columns: ColumnsType<WebhookEvent> = [
    {
      title: 'Date',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => new Date(date).toLocaleString(),
      sorter: (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
    },
    {
      title: 'Event Type',
      dataIndex: 'eventType',
      key: 'eventType',
    },
    {
      title: 'Provider',
      dataIndex: 'provider',
      key: 'provider',
      render: (provider: string) => <Tag>{provider}</Tag>,
    },
    {
      title: 'Status',
      key: 'status',
      render: (_: any, record: WebhookEvent) => getStatusTag(record.processed ? 'Processed' : 'Pending'),
    },
    {
      title: 'Tenant ID',
      dataIndex: 'tenantId',
      key: 'tenantId',
      render: (id: number | null) => id ? <Tag color="blue">{id}</Tag> : <Tag>N/A</Tag>,
    },
    {
      title: 'Subscription ID',
      dataIndex: 'subscriptionId',
      key: 'subscriptionId',
      render: (id: number | null) => id ? <Tag color="purple">{id}</Tag> : <Tag>N/A</Tag>,
    },
    {
      title: 'Actions',
      key: 'actions',
      width: 120,
      render: (_: any, record: WebhookEvent) => (
        <Button
          size="small"
          icon={<FileTextOutlined />}
          onClick={() => {
            Modal.info({
              title: 'Webhook Event Details',
              width: 800,
              content: (
                <div>
                  <p><strong>Event Type:</strong> {record.eventType}</p>
                  <p><strong>Provider:</strong> {record.provider}</p>
                  <p><strong>Status:</strong> {record.processed ? 'Processed' : 'Pending'}</p>
                  <p><strong>Created:</strong> {new Date(record.createdAt).toLocaleString()}</p>
                  {record.error && (
                    <p><strong>Error:</strong> <Tag color="red">{record.error}</Tag></p>
                  )}
                  <pre style={{ background: '#f5f5f5', padding: 12, borderRadius: 4, maxHeight: 400, overflow: 'auto' }}>
                    {JSON.stringify(JSON.parse(record.payload), null, 2)}
                  </pre>
                </div>
              ),
            });
          }}
        >
          View
        </Button>
      ),
    },
  ];

  return (
    <div>
      <PageHeader title="Billing Center" />

      <Row gutter={16} style={{ marginBottom: 16 }}>
        <Col span={6}>
          <Card>
            <Statistic
              title="Total Webhooks"
              value={webhooks.length}
              prefix={<FileTextOutlined />}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="Processed"
              value={webhooks.filter(w => w.processed).length}
              prefix={<CheckCircleOutlined />}
              valueStyle={{ color: '#3f8600' }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="Failed"
              value={webhooks.filter(w => !w.processed && w.error).length}
              prefix={<CloseCircleOutlined />}
              valueStyle={{ color: '#cf1322' }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="Pending"
              value={webhooks.filter(w => !w.processed && !w.error).length}
              prefix={<ReloadOutlined />}
              valueStyle={{ color: '#faad14' }}
            />
          </Card>
        </Col>
      </Row>

      <Card>
        <Row gutter={16} style={{ marginBottom: 16 }}>
          <Col span={6}>
            <DatePicker
              placeholder="Start Date"
              style={{ width: '100%' }}
              value={startDate}
              onChange={setStartDate}
            />
          </Col>
          <Col span={6}>
            <DatePicker
              placeholder="End Date"
              style={{ width: '100%' }}
              value={endDate}
              onChange={setEndDate}
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
              <Select.Option value="Processed">Processed</Select.Option>
              <Select.Option value="Pending">Pending</Select.Option>
              <Select.Option value="Failed">Failed</Select.Option>
            </Select>
          </Col>
          <Col span={4}>
            <Select
              placeholder="Provider"
              style={{ width: '100%' }}
              allowClear
              value={providerFilter}
              onChange={setProviderFilter}
            >
              <Select.Option value="Stripe">Stripe</Select.Option>
              <Select.Option value="Paddle">Paddle</Select.Option>
            </Select>
          </Col>
          <Col span={4}>
            <Space>
              <Button icon={<ReloadOutlined />} onClick={fetchWebhookEvents} loading={loading}>
                Refresh
              </Button>
              <Button
                type="primary"
                icon={<CheckCircleOutlined />}
                onClick={handleReconcile}
                loading={reconciling}
              >
                Reconcile
              </Button>
            </Space>
          </Col>
        </Row>

        <Table
          columns={columns}
          dataSource={webhooks}
          rowKey="id"
          loading={loading}
          pagination={{ pageSize: 20, showTotal: (total) => `Total ${total} webhook events` }}
        />
      </Card>
    </div>
  );
}

