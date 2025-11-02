import { useState, useEffect, useCallback } from 'react';
import { Table, Input, Select, Button, Space, Tag, Popconfirm, message, Tooltip } from 'antd';
import {
  SearchOutlined,
  EditOutlined,
  EyeOutlined,
  DeleteOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import type { ColumnsType, TableProps } from 'antd/es/table';
import { employeeAPI, tenantAPI } from '../../services/api';
import { useNotification } from '../../contexts/NotificationContext';
import { useAuthStore } from '../../store/authStore';
import type { Employee, EmployeeSearchRequest, PagedResponse } from '../../types/api';

const { Search } = Input;

interface EmployeeListProps {
  departments: string[];
  positions: string[];
  onEdit: (employee: Employee) => void;
  onView: (employee: Employee) => void;
  onDelete: (id: number) => void;
  onRefresh?: () => void;
}

export default function EmployeeList({
  departments,
  positions,
  onEdit,
  onView,
  onDelete,
  onRefresh,
}: EmployeeListProps) {
  const notify = useNotification();
  const { user } = useAuthStore();
  const isSuperAdmin = user?.roles?.[0] === 'SuperAdmin';
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(false);
  const [tenants, setTenants] = useState<any[]>([]);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [pagination, setPagination] = useState({
    current: 1,
    pageSize: 10,
    total: 0,
    showSizeChanger: true,
    showTotal: (total: number) => `Total ${total} employees`,
    pageSizeOptions: ['10', '25', '50', '100'],
  });

  // Search and filter state
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [departmentFilter, setDepartmentFilter] = useState<string | undefined>(undefined);
  const [positionFilter, setPositionFilter] = useState<string | undefined>(undefined);
  const [statusFilter, setStatusFilter] = useState<boolean | undefined>(undefined);
  const [tenantFilter, setTenantFilter] = useState<string | undefined>(undefined);
  const [sortBy, setSortBy] = useState<string>('FirstName');
  const [sortDirection, setSortDirection] = useState<string>('asc');

  // Fetch tenants for SuperAdmin
  useEffect(() => {
    if (isSuperAdmin) {
      const fetchTenants = async () => {
        try {
          const response = await tenantAPI.getAll();
          setTenants(response.data);
        } catch (error) {
          console.error('Failed to fetch tenants:', error);
        }
      };
      fetchTenants();
    }
  }, [isSuperAdmin]);

  const fetchEmployees = useCallback(async () => {
    setLoading(true);
    try {
      const searchRequest: EmployeeSearchRequest = {
        searchTerm: searchTerm || undefined,
        department: departmentFilter,
        position: positionFilter,
        isActive: statusFilter,
        pageNumber: pagination.current,
        pageSize: pagination.pageSize,
        sortBy,
        sortDirection,
        tenantId: isSuperAdmin ? tenantFilter : undefined,
      };

      const response = await employeeAPI.search(searchRequest);
      const data: PagedResponse<Employee> = response.data;

      setEmployees(data.data);
      setPagination(prev => ({
        ...prev,
        total: data.totalCount,
        current: data.pageNumber,
      }));
    } catch (error: any) {
      notify.error('Failed to Load Employees', error.response?.data?.message || 'Unable to fetch employees.');
      console.error('Error fetching employees:', error);
    } finally {
      setLoading(false);
    }
  }, [searchTerm, departmentFilter, positionFilter, statusFilter, tenantFilter, pagination.current, pagination.pageSize, sortBy, sortDirection, isSuperAdmin, notify]);

  useEffect(() => {
    fetchEmployees();
  }, [fetchEmployees]);

  const handleTableChange: TableProps<Employee>['onChange'] = (newPagination, _filters, sorter) => {
    const paginationInfo = {
      ...pagination,
      current: newPagination.current || 1,
      pageSize: newPagination.pageSize || 10,
    };
    setPagination(paginationInfo);

    // Handle sorting
    if (Array.isArray(sorter) && sorter.length > 0) {
      const sortInfo = sorter[0];
      if (sortInfo.order) {
        setSortBy(sortInfo.field as string);
        setSortDirection(sortInfo.order === 'ascend' ? 'asc' : 'desc');
      }
    } else if (sorter && 'field' in sorter && sorter.field) {
      if (sorter.order) {
        setSortBy(sorter.field as string);
        setSortDirection(sorter.order === 'ascend' ? 'asc' : 'desc');
      }
    }
  };

  const handleSearch = (value: string) => {
    setSearchTerm(value);
    setPagination(prev => ({ ...prev, current: 1 }));
  };

  const handleFilterChange = () => {
    setPagination(prev => ({ ...prev, current: 1 }));
    fetchEmployees();
  };

  const handleBulkActivate = async () => {
    if (selectedRowKeys.length === 0) {
      message.warning('Please select employees to activate.');
      return;
    }

    setLoading(true);
    try {
      const updatePromises = selectedRowKeys.map(id =>
        employeeAPI.update(Number(id), { isActive: true } as any)
      );
      await Promise.all(updatePromises);
      notify.success('Employees Activated', `${selectedRowKeys.length} employee(s) have been activated.`);
      setSelectedRowKeys([]);
      fetchEmployees();
      onRefresh?.();
    } catch (error: any) {
      notify.error('Activation Failed', 'Failed to activate selected employees.');
    } finally {
      setLoading(false);
    }
  };

  const handleBulkDeactivate = async () => {
    if (selectedRowKeys.length === 0) {
      message.warning('Please select employees to deactivate.');
      return;
    }

    setLoading(true);
    try {
      const updatePromises = selectedRowKeys.map(id =>
        employeeAPI.update(Number(id), { isActive: false } as any)
      );
      await Promise.all(updatePromises);
      notify.success('Employees Deactivated', `${selectedRowKeys.length} employee(s) have been deactivated.`);
      setSelectedRowKeys([]);
      fetchEmployees();
      onRefresh?.();
    } catch (error: any) {
      notify.error('Deactivation Failed', 'Failed to deactivate selected employees.');
    } finally {
      setLoading(false);
    }
  };

  const columns: ColumnsType<Employee> = [
    {
      title: 'Employee ID',
      dataIndex: 'employeeId',
      key: 'employeeId',
      sorter: true,
      width: 120,
    },
    {
      title: 'Name',
      key: 'name',
      sorter: true,
      render: (_, record) => `${record.firstName} ${record.lastName}`,
      width: 200,
    },
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
      sorter: true,
      width: 220,
    },
    {
      title: 'Department',
      dataIndex: 'department',
      key: 'department',
      sorter: true,
      width: 150,
    },
    {
      title: 'Position',
      dataIndex: 'position',
      key: 'position',
      sorter: true,
      width: 180,
    },
    {
      title: 'Hire Date',
      dataIndex: 'hireDate',
      key: 'hireDate',
      sorter: true,
      render: (date: string) => new Date(date).toLocaleDateString(),
      width: 120,
    },
    {
      title: 'Status',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 100,
      render: (isActive: boolean) => (
        <Tag color={isActive ? 'green' : 'red'} icon={isActive ? <CheckCircleOutlined /> : <CloseCircleOutlined />}>
          {isActive ? 'Active' : 'Inactive'}
        </Tag>
      ),
    },
    {
      title: 'Actions',
      key: 'actions',
      fixed: 'right',
      width: 150,
      render: (_, record) => (
        <Space size="small">
          <Tooltip title="View Details">
            <Button
              type="link"
              icon={<EyeOutlined />}
              onClick={() => onView(record)}
              style={{ color: 'var(--color-primary)' }}
            />
          </Tooltip>
          <Tooltip title="Edit">
            <Button
              type="link"
              icon={<EditOutlined />}
              onClick={() => onEdit(record)}
              style={{ color: 'var(--color-primary)' }}
            />
          </Tooltip>
          <Popconfirm
            title="Delete Employee"
            description={`Are you sure you want to delete ${record.firstName} ${record.lastName}?`}
            onConfirm={() => onDelete(record.id)}
            okText="Yes"
            cancelText="No"
            okButtonProps={{ danger: true }}
          >
            <Tooltip title="Delete">
              <Button
                type="link"
                danger
                icon={<DeleteOutlined />}
              />
            </Tooltip>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const rowSelection = {
    selectedRowKeys,
    onChange: (selectedKeys: React.Key[]) => {
      setSelectedRowKeys(selectedKeys);
    },
  };

  return (
    <div>
      {/* Filters */}
      <Space direction="vertical" style={{ width: '100%', marginBottom: 16 }} size="middle">
        <Space wrap style={{ width: '100%', justifyContent: 'space-between' }}>
          <Space wrap>
            <Search
              placeholder="Search by name, email, or employee ID"
              allowClear
              enterButton={<SearchOutlined />}
              size="large"
              style={{ width: 300 }}
              onSearch={handleSearch}
              onChange={(e) => !e.target.value && handleSearch('')}
            />
            {isSuperAdmin && (
              <Select
                placeholder="Tenant"
                allowClear
                style={{ width: 200 }}
                size="large"
                value={tenantFilter}
                onChange={(value) => {
                  setTenantFilter(value);
                  setPagination(prev => ({ ...prev, current: 1 }));
                }}
                options={[
                  { value: undefined, label: 'All Tenants' },
                  ...tenants.map(t => ({ value: t.name || String(t.id), label: t.name || `Tenant ${t.id}` }))
                ]}
              />
            )}
            <Select
              placeholder="Department"
              allowClear
              style={{ width: 150 }}
              size="large"
              value={departmentFilter}
              onChange={(value) => {
                setDepartmentFilter(value);
                handleFilterChange();
              }}
              options={departments.map(dept => ({ label: dept, value: dept }))}
            />
            <Select
              placeholder="Position"
              allowClear
              style={{ width: 180 }}
              size="large"
              value={positionFilter}
              onChange={(value) => {
                setPositionFilter(value);
                handleFilterChange();
              }}
              options={positions.map(pos => ({ label: pos, value: pos }))}
            />
            <Select
              placeholder="Status"
              allowClear
              style={{ width: 120 }}
              size="large"
              value={statusFilter}
              onChange={(value) => {
                setStatusFilter(value);
                handleFilterChange();
              }}
              options={[
                { label: 'Active', value: true },
                { label: 'Inactive', value: false },
              ]}
            />
          </Space>
          <Space>
            <Button
              icon={<ReloadOutlined />}
              onClick={fetchEmployees}
              loading={loading}
            >
              Refresh
            </Button>
          </Space>
        </Space>

        {/* Bulk Actions */}
        {selectedRowKeys.length > 0 && (
          <Space>
            <Button
              type="primary"
              icon={<CheckCircleOutlined />}
              onClick={handleBulkActivate}
              loading={loading}
              style={{
                background: 'var(--gradient-primary)',
                border: 'none',
              }}
            >
              Activate ({selectedRowKeys.length})
            </Button>
            <Button
              danger
              icon={<CloseCircleOutlined />}
              onClick={handleBulkDeactivate}
              loading={loading}
            >
              Deactivate ({selectedRowKeys.length})
            </Button>
          </Space>
        )}
      </Space>

      {/* Table */}
      <Table
        columns={columns}
        dataSource={employees}
        rowKey="id"
        loading={loading}
        pagination={pagination}
        onChange={handleTableChange}
        rowSelection={rowSelection}
        scroll={{ x: 1200 }}
        style={{ background: 'transparent' }}
      />
    </div>
  );
}

