import { useState, useEffect } from 'react';
import { Button, Space } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import EmployeeList from '../components/Employee/EmployeeList';
import EmployeeForm from '../components/Employee/EmployeeForm';
import EmployeeDetail from '../components/Employee/EmployeeDetail';
import { useNotification } from '../contexts/NotificationContext';
import { employeeAPI, departmentAPI, positionAPI } from '../services/api';
import type { Employee } from '../types/api';

export type EmployeeViewMode = 'list' | 'create' | 'edit' | 'detail';

export default function Employees() {
  const notify = useNotification();
  const [viewMode, setViewMode] = useState<EmployeeViewMode>('list');
  const [selectedEmployee, setSelectedEmployee] = useState<Employee | null>(null);
  const [departments, setDepartments] = useState<string[]>([]);
  const [positions, setPositions] = useState<string[]>([]);

  // Fetch unique departments and positions for filters
  useEffect(() => {
    fetchMetadata();
  }, []);

  const fetchMetadata = async () => {
    try {
      // Fetch departments and positions from APIs
      const [departmentsResponse, positionsResponse] = await Promise.all([
        departmentAPI.getNames(),
        positionAPI.getTitles(),
      ]);
      
      setDepartments(departmentsResponse.data.sort());
      setPositions(positionsResponse.data.sort());
    } catch (error: any) {
      console.error('Failed to fetch metadata:', error);
      // Fallback: extract from employees if API fails
      try {
        const response = await employeeAPI.getAll();
        const employees = response.data;
        const uniqueDepartments = Array.from(new Set(employees.map(e => e.department).filter(Boolean)));
        const uniquePositions = Array.from(new Set(employees.map(e => e.position).filter(Boolean)));
        setDepartments(uniqueDepartments.sort());
        setPositions(uniquePositions.sort());
      } catch (fallbackError) {
        console.error('Fallback metadata fetch also failed:', fallbackError);
      }
    }
  };

  const handleCreate = () => {
    setSelectedEmployee(null);
    setViewMode('create');
  };

  const handleEdit = (employee: Employee) => {
    setSelectedEmployee(employee);
    setViewMode('edit');
  };

  const handleView = (employee: Employee) => {
    setSelectedEmployee(employee);
    setViewMode('detail');
  };

  const handleBackToList = () => {
    setSelectedEmployee(null);
    setViewMode('list');
  };

  const handleCreateSuccess = async (employee: Employee) => {
    notify.success('Employee Created', `${employee.firstName} ${employee.lastName} has been created successfully.`);
    await fetchMetadata(); // Refresh metadata for new departments/positions
    setViewMode('list');
  };

  const handleUpdateSuccess = async (employee: Employee) => {
    notify.success('Employee Updated', `${employee.firstName} ${employee.lastName} has been updated successfully.`);
    await fetchMetadata(); // Refresh metadata for new departments/positions
    setViewMode('list');
  };

  const handleDelete = async (id: number) => {
    try {
      await employeeAPI.delete(id);
      notify.success('Employee Deleted', 'Employee has been deleted successfully.');
      if (selectedEmployee?.id === id) {
        setViewMode('list');
        setSelectedEmployee(null);
      }
    } catch (error: any) {
      notify.error('Delete Failed', error.response?.data?.message || 'Failed to delete employee.');
    }
  };

  return (
    <div>
      {viewMode === 'list' && (
        <>
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
                fontFamily: 'var(--button-font-family)',
                fontWeight: 'var(--button-font-weight)',
                boxShadow: 'var(--button-shadow-primary)',
              }}
            >
              Create New Employee
            </Button>
          </Space>
          <EmployeeList
            departments={departments}
            positions={positions}
            onEdit={handleEdit}
            onView={handleView}
            onDelete={handleDelete}
            onRefresh={fetchMetadata}
          />
        </>
      )}

      {viewMode === 'create' && (
        <>
          <EmployeeForm
            mode="create"
            departments={departments}
            positions={positions}
            onSuccess={handleCreateSuccess}
            onCancel={handleBackToList}
          />
        </>
      )}

      {viewMode === 'edit' && selectedEmployee && (
        <>
          <EmployeeForm
            mode="edit"
            employee={selectedEmployee}
            departments={departments}
            positions={positions}
            onSuccess={handleUpdateSuccess}
            onCancel={handleBackToList}
          />
        </>
      )}

      {viewMode === 'detail' && selectedEmployee && (
        <>
          <EmployeeDetail
            employee={selectedEmployee}
            onEdit={() => handleEdit(selectedEmployee)}
            onBack={handleBackToList}
          />
        </>
      )}
    </div>
  );
}

