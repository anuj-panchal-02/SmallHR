# 👤 Employee User Creation Guide

## Overview

When you create an employee, a user account is **automatically created** with login credentials.

---

## 🔐 Password Format

### Default Password Format
```
Welcome@{EmployeeId}123!
```

### Examples
- Employee ID: `KM001` → Password: `Welcome@KM001123!`
- Employee ID: `EMP001` → Password: `Welcome@EMP001123!`
- Employee ID: `HR002` → Password: `Welcome@HR002123!`

### Password Requirements
- ✅ Minimum 12 characters
- ✅ Contains uppercase letter (W)
- ✅ Contains lowercase letters (elcome)
- ✅ Contains numbers (123)
- ✅ Contains special characters (@ and !)

---

## 📝 Login Credentials

When an employee is created, the user account has:

| Field | Value |
|-------|-------|
| **Email** | Same as employee email |
| **Password** | `Welcome@{EmployeeId}123!` |
| **Role** | Same as employee role (Employee, HR, Admin) |
| **Status** | Active by default |

---

## 🔍 How to Find the Password

### Method 1: Check API Logs
When creating an employee, check the API console/logs. You'll see:
```
LOGIN CREDENTIALS - Email: {email}, Password: Welcome@{EmployeeId}123!
```

### Method 2: Calculate from Employee ID
Simply use the formula: `Welcome@{EmployeeId}123!`

### Method 3: Check Database
The password is stored as a hash in `AspNetUsers` table, so you cannot retrieve it directly. Use Method 1 or 2.

---

## 🚨 Troubleshooting

### Issue: "Invalid credentials" when logging in

**Possible Causes:**

1. **Wrong Password Format**
   - ✅ Make sure you're using: `Welcome@{EmployeeId}123!`
   - ✅ Check that EmployeeId matches exactly (case-sensitive in some systems)
   - ❌ Don't use spaces or special characters in EmployeeId part

2. **Password Was Reset**
   - If someone reset the password, it's no longer the default password
   - Use the reset password functionality in User Management

3. **User Account Status**
   - Check if `IsActive = true` in the user account
   - Inactive users cannot login

4. **Email Mismatch**
   - Make sure you're using the exact email used when creating the employee

### Solution: Reset Password

If you need to reset the password:

1. **Via API (Swagger/Postman):**
   ```
   POST /api/usermanagement/reset-password/{userId}
   Body: { "newPassword": "Welcome@KM001123!" }
   ```

2. **Via Frontend:**
   - Go to Super Admin Dashboard
   - Find the user
   - Use "Reset Password" option
   - Set new password: `Welcome@{EmployeeId}123!`

---

## 📋 Verification Checklist

After creating an employee, verify:

- [ ] User exists in `AspNetUsers` table
- [ ] Employee has `UserId` foreign key set
- [ ] User has correct role assigned
- [ ] User `IsActive = true`
- [ ] Password format: `Welcome@{EmployeeId}123!`
- [ ] Email matches employee email exactly

---

## 💡 Best Practices

1. **Document Default Passwords**
   - Keep a secure list of employee IDs and their default passwords
   - Only share with authorized personnel

2. **First Login**
   - Recommend changing password on first login
   - Or implement a "force password change" feature

3. **Password Reset Policy**
   - After password reset, inform user of new password
   - Or implement email-based password reset

4. **Security**
   - Default passwords meet complexity requirements
   - But they're predictable - users should change them

---

## 🔗 Related Files

- `SmallHR.Infrastructure/Services/EmployeeService.cs` - User creation logic
- `SmallHR.API/Controllers/UserManagementController.cs` - Password reset endpoint
- `SmallHR.Core/Entities/Employee.cs` - Employee entity with UserId foreign key

---

**Last Updated:** 2025-11-01

