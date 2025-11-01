# 🚀 SmallHR - SaaS HR Management System

A comprehensive, multi-tenant HR management system built with ASP.NET Core 8 and React.

---

## 📚 Documentation

All documentation has been organized in the **[README](./README/)** folder.

### 🔗 Quick Access

- **[📖 Full Documentation Index](./README/README.md)** - Complete documentation index
- **[🔒 Security Audit Report](./README/SECURITY_AUDIT_REPORT.md)** - Security findings and fixes
- **[🚀 Quick Start](./README/QUICK_START_JWT_SECRET.md)** - JWT secret setup
- **[🔧 API Troubleshooting](./README/API_TROUBLESHOOTING.md)** - Troubleshooting guide
- **[🌐 Frontend Documentation](./README/Frontend/)** - Frontend guides and references

---

## ⚡ Quick Start

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

**For detailed setup instructions, see [README/README.md](./README/README.md)**

