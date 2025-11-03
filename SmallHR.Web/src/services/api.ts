import axios from 'axios';
import { getApiBase } from '../utils/api';
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  Employee,
  CreateEmployeeRequest,
  UpdateEmployeeRequest,
  EmployeeSearchRequest,
  PagedResponse,
  LeaveRequest,
  CreateLeaveRequestRequest,
  Attendance,
  ClockInRequest,
  ClockOutRequest,
  Department,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
  Position,
  CreatePositionRequest,
  UpdatePositionRequest,
} from '../types/api';

const API_BASE_URL = (() => {
  const base = getApiBase();
  return base ? `${base}/api` : '/api';
})();

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Include cookies in all requests
});

// Token refresh state
let isRefreshing = false;
let pendingRequests: Array<(token: string | null) => void> = [];

const onTokenRefreshed = (newToken: string | null) => {
  pendingRequests.forEach(cb => cb(newToken));
  pendingRequests = [];
};

// Request interceptor
// Note: Tokens are now sent via httpOnly cookies automatically
// The JwtCookieMiddleware on the backend extracts cookies and adds them to the Authorization header
api.interceptors.request.use(
  (config) => {
    // Token is now sent via httpOnly cookie automatically
    config.withCredentials = true; // Ensure credentials are included
    return config;
  },
  (error) => Promise.reject(error)
);

// Helper: perform token refresh
// Refresh token is in httpOnly cookie, backend handles it
async function refreshAuthToken(): Promise<string | null> {
  try {
    const resp = await axios.post<AuthResponse>(
      `${API_BASE_URL}/auth/refresh-token`,
      {},
      { withCredentials: true }
    );
    // Tokens are set in cookies by backend, not returned in response
    return resp.data.token || null;
  } catch (error) {
    throw new Error('Token refresh failed');
  }
}

// Response interceptor to handle errors (401 -> try refresh once)
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    const status = error.response?.status;

    // If unauthorized, attempt refresh once
    if (status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      if (isRefreshing) {
        // Queue until refresh completes
        return new Promise((resolve, reject) => {
          pendingRequests.push((newToken) => {
            if (!newToken) {
              reject(error);
              return;
            }
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            resolve(api(originalRequest));
          });
        });
      }

      try {
        isRefreshing = true;
        const newToken = await refreshAuthToken();
        isRefreshing = false;
        onTokenRefreshed(newToken);
        // Retry original request (token is in cookie)
        return api(originalRequest);
      } catch (refreshErr) {
        isRefreshing = false;
        onTokenRefreshed(null);
        // Refresh failed: redirect to login
        window.location.href = '/login';
        return Promise.reject(refreshErr);
      }
    }

    return Promise.reject(error);
  }
);

// Auth API
export const authAPI = {
  login: (data: LoginRequest) =>
    api.post<AuthResponse>('/auth/login', data),
  
  register: (data: RegisterRequest) =>
    api.post<AuthResponse>('/auth/register', data),
  
  getCurrentUser: () =>
    api.get('/auth/me'),
  
  refreshToken: () =>
    api.post<AuthResponse>('/auth/refresh-token', {}),
  
  logout: () =>
    api.post('/auth/logout'),
  
  verifyEmail: (token: string, userId: string) =>
    api.post(`/auth/verify-email?token=${encodeURIComponent(token)}&userId=${encodeURIComponent(userId)}`),
  
  resendVerification: (email: string) =>
    api.post('/auth/resend-verification', { email }),
  
  forgotPassword: (email: string) =>
    api.post('/auth/forgot-password', { email }),
  
  resetPassword: (data: { email: string; token: string; newPassword: string }) =>
    api.post('/auth/reset-password', data),
  
  setupPassword: (data: { userId: string; token: string; newPassword: string }) =>
    api.post('/auth/setup-password', data),
};

// Employee API
export const employeeAPI = {
  getAll: () =>
    api.get<Employee[]>('/employees'),
  
  getById: (id: number) =>
    api.get<Employee>(`/employees/${id}`),
  
  search: (params: EmployeeSearchRequest) =>
    api.get<PagedResponse<Employee>>('/employees/search', { params }),
  
  searchPost: (data: EmployeeSearchRequest) =>
    api.post<PagedResponse<Employee>>('/employees/search', data),
  
  create: (data: CreateEmployeeRequest) =>
    api.post<Employee>('/employees', data),
  
  update: (id: number, data: UpdateEmployeeRequest) =>
    api.put<Employee>(`/employees/${id}`, data),
  
  delete: (id: number) =>
    api.delete(`/employees/${id}`),
  
  getActive: () =>
    api.get<Employee[]>('/employees/active'),
  
  getByDepartment: (department: string) =>
    api.get<Employee[]>(`/employees/by-department/${department}`),
};

// Leave Request API
export const leaveRequestAPI = {
  getAll: () =>
    api.get<LeaveRequest[]>('/leaverequests'),
  
  getById: (id: number) =>
    api.get<LeaveRequest>(`/leaverequests/${id}`),
  
  getByEmployee: (employeeId: number) =>
    api.get<LeaveRequest[]>(`/leaverequests/employee/${employeeId}`),
  
  create: (data: CreateLeaveRequestRequest) =>
    api.post<LeaveRequest>('/leaverequests', data),
  
  update: (id: number, data: Partial<CreateLeaveRequestRequest>) =>
    api.put<LeaveRequest>(`/leaverequests/${id}`, data),
  
  delete: (id: number) =>
    api.delete(`/leaverequests/${id}`),
  
  approve: (id: number, status: string, rejectionReason?: string) =>
    api.put<LeaveRequest>(`/leaverequests/${id}/approve`, {
      status,
      rejectionReason,
    }),
  
  getPending: () =>
    api.get<LeaveRequest[]>('/leaverequests/pending'),
};

// Attendance API
export const attendanceAPI = {
  getAll: () =>
    api.get<Attendance[]>('/attendance'),
  
  getById: (id: number) =>
    api.get<Attendance>(`/attendance/${id}`),
  
  getByEmployee: (employeeId: number) =>
    api.get<Attendance[]>(`/attendance/employee/${employeeId}`),
  
  clockIn: (data: ClockInRequest) =>
    api.post<Attendance>('/attendance/clock-in', data),
  
  clockOut: (data: ClockOutRequest) =>
    api.post<Attendance>('/attendance/clock-out', data),
  
  getByDateRange: (employeeId: number, startDate: string, endDate: string) =>
    api.get<Attendance[]>(`/attendance/employee/${employeeId}/date-range`, {
      params: { startDate, endDate },
    }),
  
  getByMonth: (employeeId: number, year: number, month: number) =>
    api.get<Attendance[]>(`/attendance/employee/${employeeId}/month`, {
      params: { year, month },
    }),
};

// Department API
export const departmentAPI = {
  getAll: (tenantId?: string) =>
    api.get<Department[]>('/departments', { params: tenantId ? { tenantId } : {} }),
  
  getById: (id: number) =>
    api.get<Department>(`/departments/${id}`),
  
  getNames: () =>
    api.get<string[]>('/departments/names'),
  
  create: (data: CreateDepartmentRequest) =>
    api.post<Department>('/departments', data),
  
  update: (id: number, data: UpdateDepartmentRequest) =>
    api.put<Department>(`/departments/${id}`, data),
  
  assignHead: (id: number, employeeId: number) =>
    api.put<Department>(`/departments/${id}/assign-head/${employeeId}`),
  
  removeHead: (id: number) =>
    api.put<Department>(`/departments/${id}/remove-head`),
  
  delete: (id: number) =>
    api.delete(`/departments/${id}`),
};

// Position API
export const positionAPI = {
  getAll: (tenantId?: string) =>
    api.get<Position[]>('/positions', { params: tenantId ? { tenantId } : {} }),
  
  getById: (id: number) =>
    api.get<Position>(`/positions/${id}`),
  
  getByDepartment: (departmentId: number) =>
    api.get<Position[]>(`/positions/department/${departmentId}`),
  
  getTitles: () =>
    api.get<string[]>('/positions/titles'),
  
  create: (data: CreatePositionRequest) =>
    api.post<Position>('/positions', data),
  
  update: (id: number, data: UpdatePositionRequest) =>
    api.put<Position>(`/positions/${id}`, data),
  
  delete: (id: number) =>
    api.delete(`/positions/${id}`),
};

// Tenant API
export const tenantAPI = {
  getAll: () => api.get<any[]>('/tenants'),
};

export default api;

