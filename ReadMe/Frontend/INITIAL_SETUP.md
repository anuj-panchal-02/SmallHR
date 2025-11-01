# SmallHR - Initial Setup Guide

## 🚀 Fresh SaaS Installation

SmallHR is now configured as a fresh SaaS platform with no test/demo data.

---

## 🔐 Initial SuperAdmin Account

The system creates one SuperAdmin account for initial setup:

```
Email:    superadmin@smallhr.com
Password: SuperAdmin@123
Role:     SuperAdmin
```

**⚠️ Important:** Change this password immediately after first login in production!

---

## 📋 First Steps After Installation

### 1. Login as SuperAdmin
- Use the credentials above to access the system
- Navigate to User Management to create your first Admin users
- Create Admin accounts for your team

### 2. Create Your Organization
- Use the Employees section to add your first employees
- Create departments as needed
- Set up your organizational structure

### 3. Configure System Settings
- Access Settings to configure your tenant
- Set up modules and permissions
- Customize the system for your organization

---

## 🗑️ Clean Database State

The system starts with:
- ✅ Essential roles (SuperAdmin, Admin, HR, Employee)
- ✅ One SuperAdmin user for initial access
- ❌ No demo employees
- ❌ No test leave requests
- ❌ No sample attendance records
- ❌ No demo users

**All data will be created through the application interface by your users.**

---

## 📝 Production Checklist

Before going live:
- [ ] Change SuperAdmin password
- [ ] Create your Admin users
- [ ] Configure tenant settings
- [ ] Set up permissions
- [ ] Add your first employees
- [ ] Test the workflow with your team
- [ ] Configure email notifications (if applicable)
- [ ] Set up backup procedures

---

## 🔧 Development Notes

If you want to remove the SuperAdmin seed completely:
- Edit `Program.cs`
- Comment out or remove the SuperAdmin creation section
- Users can then be created through registration or first-time setup

---

**Last Updated:** Fresh SaaS Configuration

