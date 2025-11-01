import { useState, useEffect } from 'react';
import { Button, Space, Table, Tag, Popconfirm, Modal, Form, Input, Select, Switch } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { useNotification } from '../contexts/NotificationContext';
import { positionAPI, departmentAPI } from '../services/api';
import type { Position, CreatePositionRequest, UpdatePositionRequest, Department } from '../types/api';

export default function Positions() {
  const notify = useNotification();
  const [positions, setPositions] = useState<Position[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingPosition, setEditingPosition] = useState<Position | null>(null);
  const [form] = Form.useForm();

  useEffect(() => {
    fetchPositions();
    fetchDepartments();
  }, []);

  const fetchPositions = async () => {
    setLoading(true);
    try {
      const response = await positionAPI.getAll();
      setPositions(response.data);
    } catch (error: any) {
      notify.error('Failed to Load', error.response?.data?.message || 'Failed to load positions');
    } finally {
      setLoading(false);
    }
  };

  const fetchDepartments = async () => {
    try {
      const response = await departmentAPI.getAll();
      setDepartments(response.data);
    } catch (error: any) {
      console.error('Failed to fetch departments:', error);
    }
  };

  const handleCreate = () => {
    setEditingPosition(null);
    form.resetFields();
    setIsModalOpen(true);
  };

  const handleEdit = (position: Position) => {
    setEditingPosition(position);
    form.setFieldsValue({
      title: position.title,
      departmentId: position.departmentId,
      description: position.description,
      isActive: position.isActive,
    });
    setIsModalOpen(true);
  };

  const handleDelete = async (id: number) => {
    try {
      await positionAPI.delete(id);
      notify.success('Position Deleted', 'Position has been deleted successfully');
      fetchPositions();
    } catch (error: any) {
      notify.error('Delete Failed', error.response?.data?.message || 'Failed to delete position');
    }
  };

  const handleSubmit = async (values: any) => {
    try {
      if (editingPosition) {
        const updateData: UpdatePositionRequest = {
          title: values.title,
          departmentId: values.departmentId || undefined,
          description: values.description,
          isActive: values.isActive ?? true,
        };
        await positionAPI.update(editingPosition.id, updateData);
        notify.success('Position Updated', `${values.title} has been updated successfully`);
      } else {
        const createData: CreatePositionRequest = {
          title: values.title,
          departmentId: values.departmentId || undefined,
          description: values.description,
        };
        await positionAPI.create(createData);
        notify.success('Position Created', `${values.title} has been created successfully`);
      }
      setIsModalOpen(false);
      form.resetFields();
      fetchPositions();
    } catch (error: any) {
      notify.error(
        editingPosition ? 'Update Failed' : 'Create Failed',
        error.response?.data?.message || `Failed to ${editingPosition ? 'update' : 'create'} position`
      );
    }
  };

  const columns = [
    {
      title: 'Title',
      dataIndex: 'title',
      key: 'title',
      sorter: (a: Position, b: Position) => a.title.localeCompare(b.title),
      width: 200,
    },
    {
      title: 'Department',
      dataIndex: 'departmentName',
      key: 'department',
      width: 180,
      render: (name: string | undefined) => name ? <Tag color="blue">{name}</Tag> : <Tag>Any Department</Tag>,
    },
    {
      title: 'Description',
      dataIndex: 'description',
      key: 'description',
    },
    {
      title: 'Employees',
      dataIndex: 'employeeCount',
      key: 'employeeCount',
      width: 100,
      align: 'center' as const,
      render: (count: number) => count || 0,
    },
    {
      title: 'Status',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 100,
      align: 'center' as const,
      render: (isActive: boolean) => (
        <Tag color={isActive ? 'green' : 'red'}>{isActive ? 'Active' : 'Inactive'}</Tag>
      ),
    },
    {
      title: 'Actions',
      key: 'actions',
      width: 150,
      fixed: 'right' as const,
      render: (_: any, record: Position) => (
        <Space>
          <Button
            type="link"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
          >
            Edit
          </Button>
          <Popconfirm
            title="Are you sure you want to delete this position?"
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
    <div>
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={handleCreate}
          style={{
            background: 'var(--gradient-primary)',
            border: 'none',
            height: 'var(--button-height)',
            borderRadius: 'var(--button-radius)',
          }}
        >
          Create Position
        </Button>
      </Space>
      <Table
        columns={columns}
        dataSource={positions}
        loading={loading}
        rowKey="id"
        pagination={{ pageSize: 10 }}
      />

      <Modal
        title={editingPosition ? 'Edit Position' : 'Create Position'}
        open={isModalOpen}
        onOk={() => form.submit()}
        onCancel={() => {
          setIsModalOpen(false);
          form.resetFields();
        }}
        okText={editingPosition ? 'Update' : 'Create'}
        cancelText="Cancel"
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
        >
          <Form.Item
            name="title"
            label="Title"
            rules={[{ required: true, message: 'Please enter position title' }]}
          >
            <Input placeholder="Position title" />
          </Form.Item>
          <Form.Item
            name="departmentId"
            label="Department (Optional)"
          >
            <Select
              placeholder="Select department (leave empty for any department)"
              allowClear
            >
              {departments.map(dept => (
                <Select.Option key={dept.id} value={dept.id}>
                  {dept.name}
                </Select.Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item
            name="description"
            label="Description"
          >
            <Input.TextArea rows={3} placeholder="Position description" />
          </Form.Item>
          {editingPosition && (
            <Form.Item
              name="isActive"
              label="Status"
              valuePropName="checked"
            >
              <Switch checkedChildren="Active" unCheckedChildren="Inactive" />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  );
}

