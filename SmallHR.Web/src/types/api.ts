// API Response Types
export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  dateOfBirth: string;
  address: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
  createdAt: string;
  isActive: boolean;
  roles: string[];
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiration: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
}

export interface Employee {
  id: number;
  employeeId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: string;
  hireDate: string;
  terminationDate?: string;
  position: string;
  department: string;
  salary: number;
  address: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  emergencyContactRelationship: string;
  isActive: boolean;
  role: string;
  userId?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateEmployeeRequest {
  employeeId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: string;
  hireDate: string;
  position: string;
  department: string;
  salary: number;
  address: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  emergencyContactRelationship: string;
  role: string;
}

export interface UpdateEmployeeRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: string;
  terminationDate?: string;
  position: string;
  department: string;
  salary: number;
  address: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  emergencyContactRelationship: string;
  isActive: boolean;
  role: string;
}

export interface EmployeeSearchRequest {
  searchTerm?: string;
  department?: string;
  position?: string;
  isActive?: boolean;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: string;
}

export interface PagedResponse<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface LeaveRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  startDate: string;
  endDate: string;
  leaveType: string;
  reason: string;
  comments?: string;
  status: string;
  approvedBy?: string;
  approvedAt?: string;
  rejectionReason?: string;
  totalDays: number;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateLeaveRequestRequest {
  employeeId: number;
  startDate: string;
  endDate: string;
  leaveType: string;
  reason: string;
  comments?: string;
}

export interface Attendance {
  id: number;
  employeeId: number;
  employeeName: string;
  date: string;
  clockInTime?: string;
  clockOutTime?: string;
  totalHours?: string;
  overtimeHours?: string;
  status: string;
  notes?: string;
  isHoliday: boolean;
  isWeekend: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface ClockInRequest {
  employeeId: number;
  clockInTime: string;
}

export interface ClockOutRequest {
  employeeId: number;
  clockOutTime: string;
}

export interface RolePermission {
  id: number;
  roleName: string;
  pageName: string;
  pagePath: string;
  canAccess: boolean;
  canView: boolean;
  canCreate: boolean;
  canEdit: boolean;
  canDelete: boolean;
  description?: string;
}

export interface Department {
  id: number;
  name: string;
  description?: string;
  headOfDepartmentId?: number;
  headOfDepartmentName?: string; // Computed: Employee full name
  isActive: boolean;
  employeeCount?: number;
  positions?: string[];
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateDepartmentRequest {
  name: string;
  description?: string;
  // HeadOfDepartmentId is optional - can be assigned later
}

export interface UpdateDepartmentRequest {
  name: string;
  description?: string;
  headOfDepartmentId?: number; // Employee ID - can be null to remove head
  isActive: boolean;
}

export interface Position {
  id: number;
  title: string;
  departmentId?: number;
  departmentName?: string;
  description?: string;
  isActive: boolean;
  employeeCount?: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreatePositionRequest {
  title: string;
  departmentId?: number;
  description?: string;
}

export interface UpdatePositionRequest {
  title: string;
  departmentId?: number;
  description?: string;
  isActive: boolean;
}

