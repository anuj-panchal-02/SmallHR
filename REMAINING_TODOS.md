# Remaining TODOs

## Tenant Filtering for SuperAdmin - Remaining Pages

The tenant filtering feature has been successfully implemented for the **Employees** page. The following pages still need the same tenant filtering functionality for SuperAdmin:

### ✅ Completed
- [x] **Employees Page** - Tenant filtering implemented (backend + frontend)

### 🔲 Pending
- [ ] **Departments Page** - Add tenant filter for SuperAdmin
  - Update `DepartmentsController` to accept `tenantId` parameter
  - Update `DepartmentService` and `DepartmentRepository` to handle tenant filtering
  - Add tenant filter dropdown in frontend Departments page
  
- [ ] **Positions Page** - Add tenant filter for SuperAdmin
  - Update `PositionsController` to accept `tenantId` parameter
  - Update `PositionService` and `PositionRepository` to handle tenant filtering
  - Add tenant filter dropdown in frontend Positions page
  
- [ ] **Leave Requests Page** - Add tenant filter for SuperAdmin
  - Update `LeaveRequestsController` to accept `tenantId` parameter
  - Update `LeaveRequestService` and `LeaveRequestRepository` to handle tenant filtering
  - Add tenant filter dropdown in frontend Leave Requests page
  
- [ ] **Attendance Page** - Add tenant filter for SuperAdmin
  - Update `AttendanceController` to accept `tenantId` parameter
  - Update `AttendanceService` and `AttendanceRepository` to handle tenant filtering
  - Add tenant filter dropdown in frontend Attendance page

## Implementation Pattern (Reference)

The Employees page can be used as a reference for implementing tenant filtering on other pages:

### Backend Changes:
1. Add `tenantId` parameter to controller GET methods
2. Check if user is SuperAdmin
3. For SuperAdmin:
   - If `tenantId` is null/empty → pass empty string `""` to show all tenants
   - If `tenantId` has value → use it to filter by specific tenant
4. For non-SuperAdmin → always set `tenantId` to null
5. Update service/repository to handle tenant filtering using `IgnoreQueryFilters()` when `tenantId != null`

### Frontend Changes:
1. Check if user is SuperAdmin (`user?.roles?.[0] === 'SuperAdmin'`)
2. Fetch tenants list for SuperAdmin
3. Add tenant filter dropdown (only visible for SuperAdmin)
4. Pass `tenantId` to API calls when filtering

## Notes

- All warnings in the build are non-critical (null reference warnings, async method warnings, file lock warnings)
- Build succeeds with 0 errors
- Code is ready for production use

