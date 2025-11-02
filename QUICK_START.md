# ⚡ SmallHR Quick Start Guide

Get SmallHR running in **5 minutes** on your local machine!

---

## 📋 Prerequisites

Before you begin, make sure you have:

- ✅ **Windows 10/11**
- ✅ **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- ✅ **Node.js 20+** - [Download here](https://nodejs.org/)
- ✅ **Visual Studio** (optional but recommended) - Includes SQL Server LocalDB

---

## 🚀 Quick Setup (3 Steps)

### Step 1: Run Initial Setup

Open PowerShell in the project root and run:

```powershell
.\scripts\setup-first-time.ps1
```

This will:
- ✅ Check your environment
- ✅ Setup database
- ✅ Configure JWT secrets
- ✅ Install dependencies

**First run takes 3-5 minutes** (downloading packages)

### Step 2: Start the Application

Open PowerShell in the project root and run:

```powershell
.\scripts\start-dev.ps1
```

This opens **two windows**:
- **API Server** (Terminal 1)
- **Frontend Server** (Terminal 2)

### Step 3: Login

1. Open browser: **[http://localhost:5173](http://localhost:5173)**
2. Login with:
   - **Email:** `superadmin@smallhr.com`
   - **Password:** `SuperAdmin@123`

---

## 🎯 You're Done!

You now have:
- ✅ Multi-tenant HR system running
- ✅ SaaS subscription management
- ✅ Employee management
- ✅ Leave management
- ✅ Attendance tracking

---

## 🔧 Manual Setup (Alternative)

If the automated script doesn't work for you:

### Backend
```powershell
# 1. Setup JWT secret
cd SmallHR.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "DevSecretKey_ChangeMe_ToA32+CharsRandomValue"

# 2. Run migrations
cd ..
dotnet ef database update --project SmallHR.Infrastructure --startup-project SmallHR.API

# 3. Start API
cd SmallHR.API
dotnet run
```

### Frontend (New Terminal)
```powershell
# 1. Install dependencies
cd SmallHR.Web
npm install

# 2. Start dev server
npm run dev
```

---

## 🆘 Troubleshooting

### "Can't connect to database"

**Solution:** Install SQL Server LocalDB
- Option 1: Install Visual Studio (includes LocalDB)
- Option 2: Install [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads)

### "Port already in use"

**Solution:** Kill the process using that port
```powershell
# For port 5192 (API)
Get-Process -Id (Get-NetTCPConnection -LocalPort 5192).OwningProcess | Stop-Process

# For port 5173 (Frontend)
Get-Process -Id (Get-NetTCPConnection -LocalPort 5173).OwningProcess | Stop-Process
```

### "npm install fails"

**Solution:** Clear npm cache and try again
```powershell
npm cache clean --force
npm install
```

### "dotnet command not found"

**Solution:** Install .NET 8 SDK
- Download from: https://dotnet.microsoft.com/download/dotnet/8.0
- Restart PowerShell after installation

---

## 📚 Next Steps

### For Development
- Read [Frontend/GETTING_STARTED.md](./README/Frontend/GETTING_STARTED.md)
- Review [API_TROUBLESHOOTING.md](./README/API_TROUBLESHOOTING.md)

### For Deployment
- Read [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)
- Review [Security Audit Report](./README/SECURITY_AUDIT_REPORT.md)

### For Testing
- Check [MULTI_TENANT_PENETRATION_TEST.md](./README/MULTI_TENANT_PENETRATION_TEST.md)

---

## 🔐 Important Security Notes

⚠️ **Before deploying to production:**

1. Change default SuperAdmin password
2. Generate a new strong JWT secret (32+ characters)
3. Configure proper database connection
4. Enable HTTPS
5. Review security audit: [CRITICAL_FIXES_APPLIED.md](./README/CRITICAL_FIXES_APPLIED.md)

---

## 📞 Need Help?

1. Check [API_TROUBLESHOOTING.md](./README/API_TROUBLESHOOTING.md)
2. Review [Full Documentation](./README/README.md)
3. Check [Security Audit Report](./README/SECURITY_AUDIT_REPORT.md)

---

## 🎉 What You Get

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

**Enjoy building with SmallHR! 🚀**


