# 👑 SuperAdmin Control Panel Guide

## 🔐 **Your Special SuperAdmin Account**

```
Email:    superadmin@smallhr.com
Password: SuperAdmin@123
Role:     SuperAdmin
```

**⚠️ This account is ONLY for you!** It has full control over the entire system.

---

## 🎯 **What You Can Do**

### **1. View All Users**
- See every user in the system
- View their roles, status, and creation date
- Filter and search users

### **2. Create New Users**
Click **"Create New User"** button to add:
- Email
- Password
- First Name & Last Name
- Date of Birth
- **Select Role:**
  - **SuperAdmin** - Full system control (like you!)
  - **Admin** - Employee management, system metrics
  - **HR** - Leave & attendance management
  - **Employee** - Personal workspace

### **3. Change User Roles**
- Click **"Change Role"** next to any user
- Switch them between SuperAdmin, Admin, HR, or Employee
- They'll see a different dashboard immediately on next login

### **4. Reset Passwords**
- Click **"Reset Password"** for any user
- Set a new password for them
- Useful if they forget their password

### **5. Activate/Deactivate Users**
- Click **"Deactivate"** to disable a user's access
- Click **"Activate"** to re-enable them
- Inactive users cannot login

---

## 📊 **Role Hierarchy & Permissions**

### **👑 SuperAdmin (YOU) - UNLIMITED ACCESS**
- ✅ **Create/Edit/Delete users**
- ✅ **Assign/Change roles**
- ✅ **Reset passwords**
- ✅ **View ALL system data**
- ✅ **Access User Management panel**
- ✅ **Full access to ALL API endpoints**
- ✅ **Manage employees (Create/Edit/Delete)**
- ✅ **Approve/Reject leave requests**
- ✅ **View/Edit attendance records**
- ✅ **Access all departments**
- ✅ **Manage payroll & expenses**
- ✅ **Full system settings access**
- ✅ **No restrictions whatsoever**

### **👨‍💼 Admin**
- ✅ View all employees
- ✅ Add/Edit employees
- ✅ View system metrics
- ✅ Approve leave requests
- ❌ Cannot create users
- ❌ Cannot change roles

### **👩‍💼 HR**
- ✅ Manage leave requests
- ✅ Track attendance
- ✅ Generate reports
- ❌ Cannot add employees
- ❌ Cannot access admin features

### **👤 Employee**
- ✅ View personal data
- ✅ Request leave
- ✅ Clock in/out
- ❌ Cannot view other employees
- ❌ No management access

---

## 🚀 **How to Use**

### **Step 1: Login**
1. Go to `http://localhost:5173`
2. Enter: `superadmin@smallhr.com`
3. Password: `SuperAdmin@123`
4. Click "Login Now"

### **Step 2: Access Control Panel**
- You'll automatically see the **Super Admin Control Panel**
- This page shows ALL users in the system

### **Step 3: Create a New User**
1. Click **"Create New User"** (top right)
2. Fill in the form:
   - Email: `newuser@smallhr.com`
   - Password: `Password@123`
   - First Name: `John`
   - Last Name: `Doe`
   - Date of Birth: Select date
   - **Role**: Choose from dropdown
3. Click **"Create User"**
4. Done! User can now login

### **Step 4: Manage Existing Users**
- **Change Role**: Switch user between roles
- **Reset Password**: Set new password
- **Deactivate**: Temporarily disable access

---

## 🎨 **Dashboard Features**

### **User Table Columns:**
- **Name** - Full name
- **Email** - Login email
- **Role** - Color-coded badges:
  - 🟣 Purple = SuperAdmin
  - 🔴 Red = Admin
  - 🟠 Orange = HR
  - 🟢 Green = Employee
- **Status** - Active (✅) or Inactive (❌)
- **Created** - Account creation date
- **Actions** - Management buttons

### **Color-Coded Status:**
- **Success (Green)** - Active user
- **Error (Red)** - Inactive user
- **Purple Badge** - SuperAdmin (highest level)

---

## 🔒 **Security Features**

### **Protected Access**
- Only SuperAdmin role can access this panel
- Regular Admins, HR, and Employees **CANNOT** see this
- Requires valid JWT token

### **API Endpoints (SuperAdmin Only):**
```
GET    /api/usermanagement/users          - Get all users
GET    /api/usermanagement/roles          - Get all roles
POST   /api/usermanagement/create-user    - Create user
PUT    /api/usermanagement/update-role    - Change role
PUT    /api/usermanagement/toggle-status  - Enable/Disable
POST   /api/usermanagement/reset-password - Reset password
```

---

## 💡 **Common Use Cases**

### **Scenario 1: Add a New HR Manager**
1. Create New User
2. Email: `newhr@smallhr.com`
3. Select Role: **HR**
4. They can now manage leave requests!

### **Scenario 2: Promote Employee to Admin**
1. Find the employee in the table
2. Click **"Change Role"**
3. Select: **Admin**
4. They now have admin dashboard!

### **Scenario 3: Reset Forgotten Password**
1. User emails you: "I forgot my password"
2. Find their account
3. Click **"Reset Password"**
4. Set new password: `NewPass@123`
5. Email them the new password

### **Scenario 4: Temporarily Disable User**
1. User on leave for 6 months
2. Click **"Deactivate"**
3. They cannot login
4. When they return, click **"Activate"**

---

## 📋 **Current System Users**

After fresh database seed, you have:

1. **superadmin@smallhr.com** (SuperAdmin) - **YOU**
2. **admin@smallhr.com** (Admin) - Regular admin
3. **hr@smallhr.com** (HR) - HR manager
4. **employee@smallhr.com** (Employee) - Regular employee

---

## ⚠️ **Important Notes**

### **DO:**
- ✅ Use this to create accounts for your team
- ✅ Assign appropriate roles based on job function
- ✅ Reset passwords when users forget them
- ✅ Deactivate accounts when people leave

### **DON'T:**
- ❌ Give everyone SuperAdmin access
- ❌ Share your SuperAdmin credentials
- ❌ Delete users (deactivate instead)
- ❌ Change your own password without remembering it!

---

## 🛠️ **Troubleshooting**

### **Can't see Control Panel?**
- Make sure you logged in with `superadmin@smallhr.com`
- Check browser console for errors
- Verify backend is running

### **Create User fails?**
- Email must be unique
- Password must be at least 6 characters
- Must include: uppercase, lowercase, number
- Check backend logs for details

### **Role change not working?**
- User must logout and login again to see new dashboard
- Clear browser cache if needed

---

## 🎉 **You're All Set!**

Your SuperAdmin panel is ready! You now have complete control over user management and role assignments.

**Need to create more users?** Just login and use the control panel! 👑

---

**Last Updated:** $(Get-Date)
**Version:** 1.0
**Access Level:** SuperAdmin Only

