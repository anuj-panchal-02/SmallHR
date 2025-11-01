import { useState, useEffect } from 'react';
import { Form, Input, DatePicker, Select, InputNumber, Switch, Button, Space, Row, Col } from 'antd';
import { SaveOutlined, CloseOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import api, { employeeAPI } from '../../services/api';
import { useNotification } from '../../contexts/NotificationContext';
import type { Employee, CreateEmployeeRequest, UpdateEmployeeRequest } from '../../types/api';

interface EmployeeFormProps {
  mode: 'create' | 'edit';
  employee?: Employee;
  departments: string[];
  positions: string[];
  onSuccess: (employee: Employee) => void;
  onCancel: () => void;
}

export default function EmployeeForm({
  mode,
  employee,
  departments,
  positions,
  onSuccess,
  onCancel,
}: EmployeeFormProps) {
  const [form] = Form.useForm();
  const notify = useNotification();
  const [loading, setLoading] = useState(false);
  const [, setIsEmployeeIdChecking] = useState(false);
  const [, setIsEmailChecking] = useState(false);
  const [roles, setRoles] = useState<string[]>([]);

  useEffect(() => {
    fetchRoles();
  }, []);

  const fetchRoles = async () => {
    try {
      const response = await api.get('/usermanagement/roles');
      setRoles(response.data || []);
    } catch (error) {
      console.error('Failed to fetch roles', error);
      // Fallback to default roles if API fails
      setRoles(['SuperAdmin', 'Admin', 'HR', 'Employee']);
    }
  };

  useEffect(() => {
    if (mode === 'edit' && employee) {
      form.setFieldsValue({
        employeeId: employee.employeeId,
        firstName: employee.firstName,
        lastName: employee.lastName,
        email: employee.email,
        phoneNumber: employee.phoneNumber,
        dateOfBirth: dayjs(employee.dateOfBirth),
        hireDate: dayjs(employee.hireDate),
        terminationDate: employee.terminationDate ? dayjs(employee.terminationDate) : undefined,
        position: employee.position,
        department: employee.department,
        salary: employee.salary,
        role: employee.role || 'Employee',
        address: employee.address,
        city: employee.city,
        state: employee.state,
        zipCode: employee.zipCode,
        country: employee.country,
        emergencyContactName: employee.emergencyContactName,
        emergencyContactPhone: employee.emergencyContactPhone,
        emergencyContactRelationship: employee.emergencyContactRelationship,
        isActive: employee.isActive,
      });
    }
  }, [mode, employee, form]);

  const handleSubmit = async (values: any) => {
    setLoading(true);
    try {
      const formattedValues = {
        ...values,
        dateOfBirth: values.dateOfBirth.format('YYYY-MM-DD'),
        hireDate: values.hireDate.format('YYYY-MM-DD'),
        terminationDate: values.terminationDate ? values.terminationDate.format('YYYY-MM-DD') : undefined,
      };

      if (mode === 'create') {
        const request: CreateEmployeeRequest = {
          employeeId: formattedValues.employeeId,
          firstName: formattedValues.firstName,
          lastName: formattedValues.lastName,
          email: formattedValues.email,
          phoneNumber: formattedValues.phoneNumber,
          dateOfBirth: formattedValues.dateOfBirth,
          hireDate: formattedValues.hireDate,
          position: formattedValues.position,
          department: formattedValues.department,
          salary: formattedValues.salary,
          address: formattedValues.address || '',
          city: formattedValues.city || '',
          state: formattedValues.state || '',
          zipCode: formattedValues.zipCode || '',
          country: formattedValues.country || '',
          emergencyContactName: formattedValues.emergencyContactName || '',
          emergencyContactPhone: formattedValues.emergencyContactPhone || '',
          emergencyContactRelationship: formattedValues.emergencyContactRelationship || '',
          role: formattedValues.role || 'Employee',
        };

        const response = await employeeAPI.create(request);
        onSuccess(response.data);
      } else if (mode === 'edit' && employee) {
        const request: UpdateEmployeeRequest = {
          firstName: formattedValues.firstName,
          lastName: formattedValues.lastName,
          email: formattedValues.email,
          phoneNumber: formattedValues.phoneNumber,
          dateOfBirth: formattedValues.dateOfBirth,
          terminationDate: formattedValues.terminationDate,
          position: formattedValues.position,
          department: formattedValues.department,
          salary: formattedValues.salary,
          address: formattedValues.address || '',
          city: formattedValues.city || '',
          state: formattedValues.state || '',
          zipCode: formattedValues.zipCode || '',
          country: formattedValues.country || '',
          emergencyContactName: formattedValues.emergencyContactName || '',
          emergencyContactPhone: formattedValues.emergencyContactPhone || '',
          emergencyContactRelationship: formattedValues.emergencyContactRelationship || '',
          isActive: formattedValues.isActive ?? true,
          role: formattedValues.role || 'Employee',
        };

        const response = await employeeAPI.update(employee.id, request);
        onSuccess(response.data);
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 
                          error.response?.data?.errors?.join(', ') ||
                          `Failed to ${mode} employee.`;
      notify.error(`${mode === 'create' ? 'Create' : 'Update'} Failed`, errorMessage);
      console.error(`Error ${mode === 'create' ? 'creating' : 'updating'} employee:`, error);
    } finally {
      setLoading(false);
    }
  };

  const checkEmployeeIdExists = async (_: any, value: string) => {
    if (!value) return Promise.resolve();
    
    if (mode === 'edit' && employee?.employeeId === value) {
      return Promise.resolve(); // Same employee ID, allow
    }

    setIsEmployeeIdChecking(true);
    try {
      const response = await employeeAPI.getAll();
      const exists = response.data.some((e: Employee) => e.employeeId === value);
      if (exists) {
        setIsEmployeeIdChecking(false);
        return Promise.reject(new Error('Employee ID already exists'));
      }
      setIsEmployeeIdChecking(false);
      return Promise.resolve();
    } catch (error) {
      setIsEmployeeIdChecking(false);
      console.error('Error checking employee ID:', error);
      return Promise.resolve(); // Don't block on error
    }
  };

  const checkEmailExists = async (_: any, value: string) => {
    if (!value) return Promise.resolve();
    
    if (mode === 'edit' && employee?.email === value) {
      return Promise.resolve(); // Same email, allow
    }

    setIsEmailChecking(true);
    try {
      const response = await employeeAPI.getAll();
      const exists = response.data.some((e: Employee) => e.email === value);
      if (exists) {
        setIsEmailChecking(false);
        return Promise.reject(new Error('Email already exists'));
      }
      setIsEmailChecking(false);
      return Promise.resolve();
    } catch (error) {
      setIsEmailChecking(false);
      console.error('Error checking email:', error);
      return Promise.resolve(); // Don't block on error
    }
  };

  return (
    <Form
      form={form}
      layout="vertical"
      onFinish={handleSubmit}
      initialValues={{
        country: 'USA',
        isActive: true,
        role: 'Employee',
      }}
    >
      {/* Basic Information */}
      <Row gutter={16}>
        <Col xs={24} sm={12} md={8}>
          <Form.Item
            label="Employee ID"
            name="employeeId"
            rules={[
              { required: mode === 'create', message: 'Please enter employee ID' },
              { validator: checkEmployeeIdExists },
            ]}
          >
            <Input 
              placeholder="EMP001" 
              disabled={mode === 'edit'}
            />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <Form.Item
            label="First Name"
            name="firstName"
            rules={[{ required: true, message: 'Please enter first name' }]}
          >
            <Input placeholder="John" />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <Form.Item
            label="Last Name"
            name="lastName"
            rules={[{ required: true, message: 'Please enter last name' }]}
          >
            <Input placeholder="Doe" />
          </Form.Item>
        </Col>
      </Row>

      <Row gutter={16}>
        <Col xs={24} sm={12} md={8}>
          <Form.Item
            label="Email"
            name="email"
            rules={[
              { required: true, message: 'Please enter email' },
              { type: 'email', message: 'Please enter a valid email' },
              { validator: checkEmailExists },
            ]}
          >
            <Input 
              type="email" 
              placeholder="john.doe@example.com"
            />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <Form.Item
            label="Phone Number"
            name="phoneNumber"
            rules={[{ required: true, message: 'Please enter phone number' }]}
          >
            <Input placeholder="555-0101" />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <Form.Item
            label="Date of Birth"
            name="dateOfBirth"
            rules={[{ required: true, message: 'Please select date of birth' }]}
          >
            <DatePicker style={{ width: '100%' }} maxDate={dayjs().subtract(18, 'year')} />
          </Form.Item>
        </Col>
      </Row>

      {/* Employment Information */}
      <Row gutter={16}>
        <Col xs={24} sm={12} md={8}>
          <Form.Item
            label="Department"
            name="department"
            rules={[{ required: true, message: 'Please select department' }]}
          >
            <Select
              placeholder="Select department"
              showSearch
              filterOption={(input, option) =>
                (option?.label ?? '').toLowerCase().includes(input.toLowerCase())
              }
              options={departments.map(dept => ({ label: dept, value: dept }))}
            />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <Form.Item
            label="Position"
            name="position"
            rules={[{ required: true, message: 'Please select position' }]}
          >
            <Select
              placeholder="Select position"
              showSearch
              filterOption={(input, option) =>
                (option?.label ?? '').toLowerCase().includes(input.toLowerCase())
              }
              options={positions.map(pos => ({ label: pos, value: pos }))}
            />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <Form.Item
            label="Salary"
            name="salary"
            rules={[
              { required: true, message: 'Please enter salary' },
              { type: 'number', min: 0, message: 'Salary must be positive' },
            ]}
          >
            <InputNumber
              style={{ width: '100%' }}
              placeholder="75000"
              formatter={value => `$ ${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
              parser={value => value!.replace(/\$\s?|(,*)/g, '') as any}
            />
          </Form.Item>
        </Col>
      </Row>

      <Row gutter={16}>
        <Col xs={24} sm={12} md={8}>
          <Form.Item
            label="Role"
            name="role"
            rules={[{ required: true, message: 'Please select role' }]}
          >
            <Select
              placeholder="Select role"
              showSearch
              filterOption={(input, option) =>
                (option?.label ?? '').toLowerCase().includes(input.toLowerCase())
              }
              options={roles.map(role => ({ label: role, value: role }))}
            />
          </Form.Item>
        </Col>
      </Row>

      <Row gutter={16}>
        <Col xs={24} sm={12} md={8}>
          <Form.Item
            label="Hire Date"
            name="hireDate"
            rules={[{ required: true, message: 'Please select hire date' }]}
          >
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
        </Col>
        {mode === 'edit' && (
          <Col xs={24} sm={12} md={8}>
            <Form.Item
              label="Termination Date"
              name="terminationDate"
            >
              <DatePicker style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        )}
        {mode === 'edit' && (
          <Col xs={24} sm={12} md={8}>
            <Form.Item
              label="Status"
              name="isActive"
              valuePropName="checked"
            >
              <Switch checkedChildren="Active" unCheckedChildren="Inactive" />
            </Form.Item>
          </Col>
        )}
      </Row>

      {/* Address Information */}
      <h3 style={{ marginTop: 24, marginBottom: 16, fontSize: 16, fontWeight: 600 }}>Address Information</h3>
      <Row gutter={16}>
        <Col xs={24}>
          <Form.Item label="Address" name="address">
            <Input placeholder="123 Main St" />
          </Form.Item>
        </Col>
      </Row>
      <Row gutter={16}>
        <Col xs={24} sm={8}>
          <Form.Item label="City" name="city">
            <Input placeholder="New York" />
          </Form.Item>
        </Col>
        <Col xs={24} sm={8}>
          <Form.Item label="State" name="state">
            <Input placeholder="NY" />
          </Form.Item>
        </Col>
        <Col xs={24} sm={8}>
          <Form.Item label="Zip Code" name="zipCode">
            <Input placeholder="10001" />
          </Form.Item>
        </Col>
      </Row>
      <Row gutter={16}>
        <Col xs={24} sm={12}>
          <Form.Item label="Country" name="country">
            <Input placeholder="USA" />
          </Form.Item>
        </Col>
      </Row>

      {/* Emergency Contact */}
      <h3 style={{ marginTop: 24, marginBottom: 16, fontSize: 16, fontWeight: 600 }}>Emergency Contact</h3>
      <Row gutter={16}>
        <Col xs={24} sm={8}>
          <Form.Item label="Contact Name" name="emergencyContactName">
            <Input placeholder="Jane Doe" />
          </Form.Item>
        </Col>
        <Col xs={24} sm={8}>
          <Form.Item label="Contact Phone" name="emergencyContactPhone">
            <Input placeholder="555-0201" />
          </Form.Item>
        </Col>
        <Col xs={24} sm={8}>
          <Form.Item label="Relationship" name="emergencyContactRelationship">
            <Select
              placeholder="Select relationship"
              options={[
                { label: 'Spouse', value: 'Spouse' },
                { label: 'Parent', value: 'Parent' },
                { label: 'Sibling', value: 'Sibling' },
                { label: 'Child', value: 'Child' },
                { label: 'Friend', value: 'Friend' },
                { label: 'Other', value: 'Other' },
              ]}
            />
          </Form.Item>
        </Col>
      </Row>

      {/* Form Actions */}
      <Form.Item style={{ marginTop: 32, marginBottom: 0 }}>
        <Space>
          <Button
            type="primary"
            htmlType="submit"
            icon={<SaveOutlined />}
            loading={loading}
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
            {mode === 'create' ? 'Create Employee' : 'Update Employee'}
          </Button>
          <Button
            icon={<CloseOutlined />}
            onClick={onCancel}
            style={{
              height: 'var(--button-height)',
              borderRadius: 'var(--button-radius)',
              fontFamily: 'var(--button-font-family)',
              fontWeight: 'var(--button-font-weight)',
            }}
          >
            Cancel
          </Button>
        </Space>
      </Form.Item>
    </Form>
  );
}

