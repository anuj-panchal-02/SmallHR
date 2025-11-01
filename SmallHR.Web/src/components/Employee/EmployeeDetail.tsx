import { Descriptions, Tag, Button, Space, Card } from 'antd';
import { EditOutlined, ArrowLeftOutlined, UserOutlined, MailOutlined, PhoneOutlined, HomeOutlined, ContactsOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import type { Employee } from '../../types/api';

interface EmployeeDetailProps {
  employee: Employee;
  onEdit: () => void;
  onBack: () => void;
}

export default function EmployeeDetail({ employee, onEdit, onBack }: EmployeeDetailProps) {
  return (
    <div>
      {/* Action Buttons */}
      <Space style={{ marginBottom: 24 }}>
        <Button
          icon={<ArrowLeftOutlined />}
          onClick={onBack}
          style={{
            height: 'var(--button-height)',
            borderRadius: 'var(--button-radius)',
            fontFamily: 'var(--button-font-family)',
            fontWeight: 'var(--button-font-weight)',
          }}
        >
          Back to List
        </Button>
        <Button
          type="primary"
          icon={<EditOutlined />}
          onClick={onEdit}
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
          Edit Employee
        </Button>
      </Space>

      {/* Basic Information Card */}
      <Card
        title={
          <Space>
            <UserOutlined />
            <span>Basic Information</span>
          </Space>
        }
        style={{
          marginBottom: 16,
          borderRadius: 12,
          boxShadow: 'var(--glass-shadow)',
          background: 'var(--glass-background)',
          border: '1px solid var(--glass-border)',
        }}
      >
        <Descriptions bordered column={{ xs: 1, sm: 2, md: 3 }}>
          <Descriptions.Item label="Employee ID" span={1}>
            {employee.employeeId}
          </Descriptions.Item>
          <Descriptions.Item label="Full Name" span={2}>
            {employee.firstName} {employee.lastName}
          </Descriptions.Item>
          <Descriptions.Item label="Email" span={1}>
            <Space>
              <MailOutlined />
              {employee.email}
            </Space>
          </Descriptions.Item>
          <Descriptions.Item label="Phone Number" span={1}>
            <Space>
              <PhoneOutlined />
              {employee.phoneNumber}
            </Space>
          </Descriptions.Item>
          <Descriptions.Item label="Date of Birth" span={1}>
            {dayjs(employee.dateOfBirth).format('MMMM DD, YYYY')}
          </Descriptions.Item>
          <Descriptions.Item label="Status" span={1}>
            <Tag color={employee.isActive ? 'green' : 'red'} icon={employee.isActive ? <UserOutlined /> : undefined}>
              {employee.isActive ? 'Active' : 'Inactive'}
            </Tag>
          </Descriptions.Item>
        </Descriptions>
      </Card>

      {/* Employment Information Card */}
      <Card
        title={
          <Space>
            <UserOutlined />
            <span>Employment Information</span>
          </Space>
        }
        style={{
          marginBottom: 16,
          borderRadius: 12,
          boxShadow: 'var(--glass-shadow)',
          background: 'var(--glass-background)',
          border: '1px solid var(--glass-border)',
        }}
      >
        <Descriptions bordered column={{ xs: 1, sm: 2, md: 3 }}>
          <Descriptions.Item label="Department" span={1}>
            {employee.department}
          </Descriptions.Item>
          <Descriptions.Item label="Position" span={1}>
            {employee.position}
          </Descriptions.Item>
          <Descriptions.Item label="Salary" span={1}>
            ${employee.salary.toLocaleString()}
          </Descriptions.Item>
          <Descriptions.Item label="Hire Date" span={1}>
            {dayjs(employee.hireDate).format('MMMM DD, YYYY')}
          </Descriptions.Item>
          {employee.terminationDate && (
            <Descriptions.Item label="Termination Date" span={1}>
              {dayjs(employee.terminationDate).format('MMMM DD, YYYY')}
            </Descriptions.Item>
          )}
          <Descriptions.Item label="Years of Service" span={1}>
            {Math.floor(dayjs().diff(dayjs(employee.hireDate), 'year', true))} years
          </Descriptions.Item>
        </Descriptions>
      </Card>

      {/* Address Information Card */}
      {(employee.address || employee.city || employee.state || employee.zipCode || employee.country) && (
        <Card
          title={
            <Space>
              <HomeOutlined />
              <span>Address Information</span>
            </Space>
          }
          style={{
            marginBottom: 16,
            borderRadius: 12,
            boxShadow: 'var(--glass-shadow)',
            background: 'var(--glass-background)',
            border: '1px solid var(--glass-border)',
          }}
        >
          <Descriptions bordered column={{ xs: 1, sm: 2 }}>
            {employee.address && (
              <Descriptions.Item label="Address" span={2}>
                {employee.address}
              </Descriptions.Item>
            )}
            {employee.city && (
              <Descriptions.Item label="City">
                {employee.city}
              </Descriptions.Item>
            )}
            {employee.state && (
              <Descriptions.Item label="State">
                {employee.state}
              </Descriptions.Item>
            )}
            {employee.zipCode && (
              <Descriptions.Item label="Zip Code">
                {employee.zipCode}
              </Descriptions.Item>
            )}
            {employee.country && (
              <Descriptions.Item label="Country">
                {employee.country}
              </Descriptions.Item>
            )}
          </Descriptions>
        </Card>
      )}

      {/* Emergency Contact Card */}
      {(employee.emergencyContactName || employee.emergencyContactPhone || employee.emergencyContactRelationship) && (
        <Card
          title={
            <Space>
              <ContactsOutlined />
              <span>Emergency Contact</span>
            </Space>
          }
          style={{
            marginBottom: 16,
            borderRadius: 12,
            boxShadow: 'var(--glass-shadow)',
            background: 'var(--glass-background)',
            border: '1px solid var(--glass-border)',
          }}
        >
          <Descriptions bordered column={{ xs: 1, sm: 2, md: 3 }}>
            {employee.emergencyContactName && (
              <Descriptions.Item label="Contact Name" span={1}>
                {employee.emergencyContactName}
              </Descriptions.Item>
            )}
            {employee.emergencyContactPhone && (
              <Descriptions.Item label="Contact Phone" span={1}>
                {employee.emergencyContactPhone}
              </Descriptions.Item>
            )}
            {employee.emergencyContactRelationship && (
              <Descriptions.Item label="Relationship" span={1}>
                {employee.emergencyContactRelationship}
              </Descriptions.Item>
            )}
          </Descriptions>
        </Card>
      )}

      {/* Metadata Card */}
      <Card
        title="System Information"
        style={{
          borderRadius: 12,
          boxShadow: 'var(--glass-shadow)',
          background: 'var(--glass-background)',
          border: '1px solid var(--glass-border)',
        }}
      >
        <Descriptions bordered column={{ xs: 1, sm: 2 }}>
          <Descriptions.Item label="Created At">
            {dayjs(employee.createdAt).format('MMMM DD, YYYY HH:mm')}
          </Descriptions.Item>
          {employee.updatedAt && (
            <Descriptions.Item label="Last Updated">
              {dayjs(employee.updatedAt).format('MMMM DD, YYYY HH:mm')}
            </Descriptions.Item>
          )}
          {employee.userId && (
            <Descriptions.Item label="Linked User ID" span={2}>
              {employee.userId}
            </Descriptions.Item>
          )}
        </Descriptions>
      </Card>
    </div>
  );
}

