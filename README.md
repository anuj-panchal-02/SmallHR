# 🚀 SmallHR - SaaS HR Management System

A comprehensive, multi-tenant HR management system built with ASP.NET Core 8 and React.

---

## ⚡ Get Started in 5 Minutes

👉 **[📖 Read the Quick Start Guide](./QUICK_START.md)**

For setup on a new machine:
```powershell
.\scripts\setup-first-time.ps1
.\scripts\start-dev.ps1
```

Then open: http://localhost:5173

**Default Login:**
- Email: `superadmin@smallhr.com`
- Password: `SuperAdmin@123`

---

## 📚 Documentation

All documentation has been organized in the **[README](./README/)** folder.

### 🔗 Quick Access

- **[⚡ Quick Start Guide](./QUICK_START.md)** - Get running in 5 minutes ⭐
- **[🚀 Deployment Guide](./DEPLOYMENT_GUIDE.md)** - Deploy to production
- **[📖 Full Documentation Index](./README/README.md)** - Complete documentation index
- **[🔒 Security Audit Report](./README/SECURITY_AUDIT_REPORT.md)** - Security findings and fixes
- **[🔧 API Troubleshooting](./README/API_TROUBLESHOOTING.md)** - Troubleshooting guide
- **[🌐 Frontend Documentation](./README/Frontend/)** - Frontend guides and references

---

## ⚡ Quick Setup (Manual)

### Prerequisites
- .NET 8 SDK
- Node.js 20+
- SQL Server (LocalDB or SQL Server)

### Backend Setup

```powershell
# Set JWT secret
cd SmallHR.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "YourSecretKeyAtLeast32CharactersLong"

# Run database migrations
dotnet ef database update --project SmallHR.Infrastructure

# Run the API
dotnet run --project SmallHR.API
```

### Frontend Setup

```powershell
cd SmallHR.Web
npm install
npm run dev
```

---

## 🎯 What You Get

SmallHR is a **complete multi-tenant SaaS HR system** with:

### ✨ Core Features
- 👥 **Employee Management** - Add, edit, track employees
- 🏢 **Department & Position Management**
- 🏖️ **Leave Management** - Request, approve, reject leaves
- ⏰ **Attendance Tracking**
- 📊 **Dashboard** - Analytics and reports

### 🏪 SaaS Features
- 🔐 **Multi-Tenancy** - Isolated data per company
- 💳 **Subscription Plans** - Free, Basic, Pro, Enterprise
- 👤 **Role-Based Access** - SuperAdmin, Admin, HR, Employee
- 🎨 **Modern UI** - Built with React + Ant Design

### 🔒 Security
- JWT authentication
- Password hashing
- SQL injection protection
- Row-level security
- CORS configuration

---

## 📋 Default Credentials

**SuperAdmin Account:**
- Email: `superadmin@smallhr.com`
- Password: `SuperAdmin@123`

⚠️ **Change this password immediately in production!**

See **[Frontend/LOGIN_CREDENTIALS.md](./README/Frontend/LOGIN_CREDENTIALS.md)** for details.

---

## 🔒 Security

This application has been audited for security vulnerabilities. See:
- **[Security Audit Report](./README/SECURITY_AUDIT_REPORT.md)**
- **[Critical Fixes Applied](./README/CRITICAL_FIXES_APPLIED.md)**

---

## 📖 Full Documentation

For complete documentation, visit the **[README](./README/)** folder which contains:
- Security audit reports and fixes
- Setup and configuration guides
- API documentation
- Frontend documentation
- Development guides
- Code quality plans

---

## 🚀 Deployment

Ready to deploy? See **[DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)**

Supported platforms:
- Windows Server + IIS
- Azure App Service
- AWS Elastic Beanstalk
- Docker (coming soon)

---

**For detailed setup instructions, see [QUICK_START.md](./QUICK_START.md)**

