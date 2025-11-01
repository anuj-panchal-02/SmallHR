# 🔧 API Troubleshooting Guide - User Management

## ✅ **FIXED: Create User Bad Request Issue**

The create user endpoint now works correctly with proper validation and error handling!

---

## 🎯 **Working Endpoint Details**

### **POST /api/usermanagement/create-user**

**URL:** `http://localhost:5192/api/usermanagement/create-user`

**Authorization:** Bearer Token (SuperAdmin role required)

**Content-Type:** `application/json`

---

## 📋 **Request Format**

### **Required Fields:**

```json
{
  "email": "user@smallhr.com",           // Required, valid email format
  "password": "Password@123",            // Required, minimum 6 characters
  "firstName": "John",                   // Required
  "lastName": "Doe",                     // Required
  "dateOfBirth": "1990-01-15T00:00:00Z", // Required, ISO 8601 format
  "role": "Employee"                     // Required, must be valid role
}
```

### **Valid Roles:**
- `SuperAdmin`
- `Admin`
- `HR`
- `Employee`

---

## ✅ **Validation Rules**

| Field | Rule | Error Message |
|-------|------|---------------|
| `email` | Required | "Email is required" |
| `email` | Valid email | "Invalid email address" |
| `password` | Required | "Password is required" |
| `password` | Min 6 chars | "Password must be at least 6 characters" |
| `firstName` | Required | "First name is required" |
| `lastName` | Required | "Last name is required" |
| `dateOfBirth` | Required | "Date of birth is required" |
| `role` | Required | "Role is required" |
| `role` | Valid role | "Role '{role}' does not exist" |
| `email` | Unique | "User with this email already exists" |

---

## 🔍 **Testing the Endpoint**

### **Method 1: PowerShell Script** ✅ (Verified Working)

Run the included test script:
```powershell
.\test-create-user.ps1
```

**Expected Output:**
```
✅ Login successful! Token received.
✅ User created successfully!
Response:
{
    "message": "User created successfully",
    "userId": "guid-here",
    "email": "testuser@smallhr.com",
    "role": "Employee"
}
```

### **Method 2: Using Swagger UI**

1. Navigate to: `http://localhost:5192/swagger`
2. Find `/api/usermanagement/create-user`
3. Click "Try it out"
4. Authorize with SuperAdmin token
5. Enter request body
6. Execute

### **Method 3: Using Postman/Insomnia**

1. **Login First:**
   - POST `http://localhost:5192/api/auth/login`
   - Body:
     ```json
     {
       "email": "superadmin@smallhr.com",
       "password": "SuperAdmin@123"
     }
     ```
   - Copy the `token` from response

2. **Create User:**
   - POST `http://localhost:5192/api/usermanagement/create-user`
   - Headers:
     - `Authorization: Bearer {your-token}`
     - `Content-Type: application/json`
   - Body:
     ```json
     {
       "email": "newuser@smallhr.com",
       "password": "Password@123",
       "firstName": "New",
       "lastName": "User",
       "dateOfBirth": "1990-01-01T00:00:00Z",
       "role": "Employee"
     }
     ```

### **Method 4: Using Frontend** (SuperAdmin Dashboard)

1. Login at `http://localhost:5173`
   - Email: `superadmin@smallhr.com`
   - Password: `SuperAdmin@123`

2. Click **"Create New User"** button
3. Fill in the form
4. Click **"Create User"**
5. Check console for detailed logs

---

## 🐛 **Common Errors & Solutions**

### **Error 1: 400 Bad Request - "Validation failed"**

**Cause:** Missing or invalid required fields

**Solution:** Check all required fields are present and valid
```json
// ❌ BAD - Missing fields
{
  "email": "test@test.com"
}

// ✅ GOOD - All fields present
{
  "email": "test@test.com",
  "password": "Test@123",
  "firstName": "Test",
  "lastName": "User",
  "dateOfBirth": "1990-01-01T00:00:00Z",
  "role": "Employee"
}
```

### **Error 2: 400 Bad Request - "Invalid email address"**

**Cause:** Email format is incorrect

**Solution:** Use proper email format
```json
// ❌ BAD
"email": "notanemail"

// ✅ GOOD
"email": "user@smallhr.com"
```

### **Error 3: 400 Bad Request - "Password must be at least 6 characters"**

**Cause:** Password is too short

**Solution:** Use minimum 6 characters
```json
// ❌ BAD
"password": "123"

// ✅ GOOD
"password": "Test@123"
```

### **Error 4: 400 Bad Request - "Role '{role}' does not exist"**

**Cause:** Invalid role name

**Solution:** Use exact role names (case-sensitive)
```json
// ❌ BAD
"role": "employee"  // lowercase
"role": "Manager"   // doesn't exist

// ✅ GOOD
"role": "Employee"  // correct case
"role": "Admin"     // valid role
```

### **Error 5: 400 Bad Request - "User with this email already exists"**

**Cause:** Email is already registered

**Solution:** Use a different email or update the existing user

### **Error 6: 401 Unauthorized**

**Cause:** Missing or invalid token

**Solution:**
1. Login first to get a valid token
2. Include token in Authorization header
3. Make sure you're logged in as SuperAdmin

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### **Error 7: 403 Forbidden**

**Cause:** User doesn't have SuperAdmin role

**Solution:**
- Only SuperAdmin can create users
- Login with: `superadmin@smallhr.com` / `SuperAdmin@123`

### **Error 8: 500 Internal Server Error**

**Cause:** Backend issue (database connection, etc.)

**Solution:**
1. Check if backend is running
2. Check database connection
3. Check backend logs for details

---

## 📊 **Success Response Format**

```json
{
  "message": "User created successfully",
  "userId": "8d2dfcfc-42db-4d2e-8ad3-b962ff821299",
  "email": "newuser@smallhr.com",
  "role": "Employee"
}
```

---

## 📊 **Error Response Format**

### **Validation Error:**
```json
{
  "message": "Validation failed",
  "errors": [
    "Email is required",
    "Password is required"
  ]
}
```

### **Business Logic Error:**
```json
{
  "message": "User with this email already exists"
}
```

### **User Creation Failed:**
```json
{
  "message": "User creation failed",
  "errors": [
    "Passwords must have at least one uppercase ('A'-'Z')",
    "Passwords must have at least one digit ('0'-'9')"
  ]
}
```

---

## 🔍 **Debugging Tips**

### **Frontend (Browser Console):**

When creating a user from the SuperAdmin dashboard, check console logs:

```javascript
📝 Creating user with values: { email: "...", password: "...", ... }
✅ User created: { message: "...", userId: "...", ... }
```

Or if error:
```javascript
❌ Create user error: AxiosError { ... }
❌ Error response: { message: "...", errors: [...] }
```

### **Backend Logs:**

Check the backend console for:

```
info: SmallHR.API.Controllers.UserManagementController[0]
      User created successfully: newuser@smallhr.com with role Employee
```

Or warnings:
```
warn: SmallHR.API.Controllers.UserManagementController[0]
      Create user validation failed: Email is required, Password is required
```

---

## 🎯 **Example: Complete User Creation Flow**

### **1. Login as SuperAdmin**

**Request:**
```http
POST http://localhost:5192/api/auth/login
Content-Type: application/json

{
  "email": "superadmin@smallhr.com",
  "password": "SuperAdmin@123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "expiration": "2025-10-28T15:30:00Z",
  "user": {
    "id": "...",
    "email": "superadmin@smallhr.com",
    "roles": ["SuperAdmin"]
  }
}
```

### **2. Create New User**

**Request:**
```http
POST http://localhost:5192/api/usermanagement/create-user
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "email": "jane.smith@smallhr.com",
  "password": "JaneSmith@123",
  "firstName": "Jane",
  "lastName": "Smith",
  "dateOfBirth": "1988-03-20T00:00:00Z",
  "role": "HR"
}
```

**Response:**
```json
{
  "message": "User created successfully",
  "userId": "f3b5c8d1-7e9a-4f2c-b6d3-8a1e9c4f5b7a",
  "email": "jane.smith@smallhr.com",
  "role": "HR"
}
```

### **3. Verify User Created**

**Request:**
```http
GET http://localhost:5192/api/usermanagement/users
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response:**
```json
[
  {
    "id": "...",
    "email": "superadmin@smallhr.com",
    "firstName": "Super",
    "lastName": "Admin",
    "isActive": true,
    "createdAt": "...",
    "roles": ["SuperAdmin"]
  },
  {
    "id": "f3b5c8d1-7e9a-4f2c-b6d3-8a1e9c4f5b7a",
    "email": "jane.smith@smallhr.com",
    "firstName": "Jane",
    "lastName": "Smith",
    "isActive": true,
    "createdAt": "...",
    "roles": ["HR"]
  }
]
```

---

## ✅ **Verification Checklist**

Before creating a user, verify:

- [ ] Backend is running (`http://localhost:5192`)
- [ ] Database is accessible
- [ ] You're logged in as SuperAdmin
- [ ] You have a valid token
- [ ] All required fields are present
- [ ] Email format is valid
- [ ] Password is at least 6 characters
- [ ] Role name is correct (case-sensitive)
- [ ] Email is not already in use
- [ ] Date format is ISO 8601 (YYYY-MM-DDTHH:mm:ssZ)

---

## 🎉 **Current Status**

✅ **FIXED AND WORKING!**

- ✅ Validation attributes added
- ✅ Improved error messages
- ✅ Better error handling
- ✅ Frontend logging enhanced
- ✅ Tested and verified working
- ✅ Complete documentation provided

**You can now create users successfully!** 🚀

---

**Last Updated:** $(Get-Date)
**Status:** ✅ RESOLVED
**Tested:** ✅ VERIFIED WORKING

