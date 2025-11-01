import { useState, useEffect } from 'react';
import { Button, Space, Table, Tag, Popconfirm, Modal, Form, Input, Select, Switch } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, UserOutlined } from '@ant-design/icons';
import { useNotification } from '../contexts/NotificationContext';
import { departmentAPI, employeeAPI } from '../services/api';
import type { Department, CreateDepartmentRequest, UpdateDepartmentRequest, Employee } from '../types/api';

export default function Departments() {
  const notify = useNotification();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isAssignHeadModalOpen, setIsAssignHeadModalOpen] = useState(false);
  const [selectedDepartmentForHead, setSelectedDepartmentForHead] = useState<Department | null>(null);
  const [editingDepartment, setEditingDepartment] = useState<Department | null>(null);
  const [form] = Form.useForm();
  const [assignHeadForm] = Form.useForm();

  useEffect(() => {
    fetchDepartments();
    fetchEmployees();
  }, []);

  const fetchDepartments = async () => {
    setLoading(true);
    try {
      const response = await departmentAPI.getAll();
      setDepartments(response.data);
    } catch (error: any) {
      notify.error('Failed to Load', error.response?.data?.message || 'Failed to load departments');
    } finally {
      setLoading(false);
    }
  };

  const fetchEmployees = async () => {
    try {
      const response = await employeeAPI.getAll();
      setEmployees(response.data);
    } catch (error: any) {
      console.error('Failed to fetch employees:', error);
    }
  };

  const handleCreate = () => {
    setEditingDepartment(null);
    form.resetFields();
    setIsModalOpen(true);
  };

  const handleEdit = (department: Department) => {
    setEditingDepartment(department);
    form.setFieldsValue({
      name: department.name,
      description: department.description,
      isActive: department.isActive,
    });
    setIsModalOpen(true);
  };

  const handleDelete = async (id: number) => {
    try {
      await departmentAPI.delete(id);
      notify.success('Department Deleted', 'Department has been deleted successfully');
      fetchDepartments();
    } catch (error: any) {
      notify.error('Delete Failed', error.response?.data?.message || 'Failed to delete department');
    }
  };

  const handleSubmit = async (values: any) => {
    try {
      if (editingDepartment) {
        const updateData: UpdateDepartmentRequest = {
          name: values.name,
          description: values.description,
          isActive: values.isActive ?? true,
        };
        await departmentAPI.update(editingDepartment.id, updateData);
        notify.success('Department Updated', `${values.name} has been updated successfully`);
      } else {
        const createData: CreateDepartmentRequest = {
          name: values.name,
          description: values.description,
        };
        await departmentAPI.create(createData);
        notify.success('Department Created', `${values.name} has been created successfully`);
      }
      setIsModalOpen(false);
      form.resetFields();
      fetchDepartments();
    } catch (error: any) {
      notify.error(
        editingDepartment ? 'Update Failed' : 'Create Failed',
        error.response?.data?.message || `Failed to ${editingDepartment ? 'update' : 'create'} department`
      );
    }
  };

  const handleAssignHeadClick = (department: Department) => {
    setSelectedDepartmentForHead(department);
    assignHeadForm.setFieldsValue({ employeeId: department.headOfDepartmentId || undefined });
    setIsAssignHeadModalOpen(true);
  };

  const handleAssignHeadSubmit = async (values: any) => {
    if (!selectedDepartmentForHead) return;
    try {
      await departmentAPI.assignHead(selectedDepartmentForHead.id, values.employeeId);
      notify.success('Head Assigned', 'Head of department has been assigned successfully');
      setIsAssignHeadModalOpen(false);
      assignHeadForm.resetFields();
      setSelectedDepartmentForHead(null);
      fetchDepartments();
    } catch (error: any) {
      notify.error('Assignment Failed', error.response?.data?.message || 'Failed to assign head of department');
    }
  };

  const handleRemoveHead = async (departmentId: number) => {
    try {
      await departmentAPI.removeHead(departmentId);
      notify.success('Head Removed', 'Head of department has been removed successfully');
      fetchDepartments();
    } catch (error: any) {
      notify.error('Remove Failed', error.response?.data?.message || 'Failed to remove head of department');
    }
  };

  const columns = [
    {
      title: 'Name',
      dataIndex: 'name',
      key: 'name',
      sorter: (a: Department, b: Department) => a.name.localeCompare(b.name),
      width: 200,
    },
    {
      title: 'Description',
      dataIndex: 'description',
      key: 'description',
    },
    {
      title: 'Head of Department',
      key: 'head',
      width: 180,
      render: (_: any, record: Department) => {
        if (record.headOfDepartmentName) {
          return <Tag color="blue">{record.headOfDepartmentName}</Tag>;
        }
        return <Tag>Not Assigned</Tag>;
      },
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
      width: 200,
      fixed: 'right' as const,
      render: (_: any, record: Department) => (
        <Space>
          <Button
            type="link"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
          >
            Edit
          </Button>
          {employees.filter(e => e.department === record.name && e.isActive).length > 0 && (
            <Button
              type="link"
              icon={<UserOutlined />}
              onClick={() => handleAssignHeadClick(record)}
            >
              {record.headOfDepartmentId ? 'Change Head' : 'Assign Head'}
            </Button>
          )}
          {record.headOfDepartmentId && (
            <Popconfirm
              title="Remove head of department?"
              onConfirm={() => handleRemoveHead(record.id)}
              okText="Yes"
              cancelText="No"
            >
              <Button type="link" danger size="small">Remove Head</Button>
            </Popconfirm>
          )}
          <Popconfirm
            title="Are you sure you want to delete this department?"
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
          Create Department
        </Button>
      </Space>
      <Table
        columns={columns}
        dataSource={departments}
        loading={loading}
        rowKey="id"
        pagination={{ pageSize: 10 }}
      />

      <Modal
        title={editingDepartment ? 'Edit Department' : 'Create Department'}
        open={isModalOpen}
        onOk={() => form.submit()}
        onCancel={() => {
          setIsModalOpen(false);
          form.resetFields();
        }}
        okText={editingDepartment ? 'Update' : 'Create'}
        cancelText="Cancel"
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
        >
          <Form.Item
            name="name"
            label="Name"
            rules={[{ required: true, message: 'Please enter department name' }]}
          >
            <Input placeholder="Department name" />
          </Form.Item>
          <Form.Item
            name="description"
            label="Description"
          >
            <Input.TextArea rows={3} placeholder="Department description" />
          </Form.Item>
          {editingDepartment && (
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

      <Modal
        title={`${selectedDepartmentForHead ? `Assign Head - ${selectedDepartmentForHead.name}` : 'Assign Head of Department'}`}
        open={isAssignHeadModalOpen}
        onOk={() => assignHeadForm.submit()}
        onCancel={() => {
          setIsAssignHeadModalOpen(false);
          assignHeadForm.resetFields();
          setSelectedDepartmentForHead(null);
        }}
        okText="Assign"
        cancelText="Cancel"
      >
        <Form
          form={assignHeadForm}
          layout="vertical"
          onFinish={handleAssignHeadSubmit}
        >
          <Form.Item
            name="employeeId"
            label="Select Employee"
            rules={[{ required: true, message: 'Please select an employee' }]}
          >
            <Select placeholder="Select employee">
              {selectedDepartmentForHead && employees
                .filter(e => e.department === selectedDepartmentForHead.name && e.isActive)
                .map(emp => (
                  <Select.Option key={emp.id} value={emp.id}>
                    {emp.firstName} {emp.lastName} ({emp.email})
                  </Select.Option>
                ))}
            </Select>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

