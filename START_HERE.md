# 👋 START HERE - SmallHR Setup Guide

Welcome to **SmallHR** - your complete multi-tenant SaaS HR management system!

---

## 🎯 What is SmallHR?

SmallHR is a fully-featured HR management system designed for SaaS deployments:

- 👥 **Employee Management**
- 🏖️ **Leave Management** 
- ⏰ **Attendance Tracking**
- 📊 **Analytics Dashboard**
- 🔐 **Multi-Tenant Architecture**
- 💳 **Subscription Management**
- 🔒 **Enterprise Security**

---

## ⚡ Fast Track Setup (5 Minutes)

### Option 1: Automated Setup (Recommended)

1. **Open PowerShell** in the project root
2. Run first-time setup:
   ```powershell
   .\scripts\setup-first-time.ps1
   ```
3. Start the application:
   ```powershell
   .\scripts\start-dev.ps1
   ```
4. Open browser: **http://localhost:5173**
5. Login:
   - Email: `superadmin@smallhr.com`
   - Password: `SuperAdmin@123`

**Done! ✅**

---

### Option 2: Manual Setup

If automated setup doesn't work, follow:

👉 **[QUICK_START.md](./QUICK_START.md)** - Detailed step-by-step guide

---

## 📚 Documentation

- **[QUICK_START.md](./QUICK_START.md)** ⭐ - Get running fast
- **[DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)** - Deploy to production
- **[README.md](./README.md)** - Full documentation index

---

## 🆘 Need Help?

**Setup Issues?**
👉 Check [QUICK_START.md](./QUICK_START.md) - Troubleshooting section

**API Problems?**
👉 See [API_TROUBLESHOOTING.md](./README/API_TROUBLESHOOTING.md)

**Security Questions?**
👉 Review [SECURITY_AUDIT_REPORT.md](./README/SECURITY_AUDIT_REPORT.md)

---

## ⚠️ Important Notes

Before deploying to production:

1. ✅ Change default SuperAdmin password
2. ✅ Generate strong JWT secret
3. ✅ Configure production database
4. ✅ Enable HTTPS
5. ✅ Review security checklist in [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)

---

## 🚀 Next Steps

1. **Run the application** (see above)
2. **Create your first tenant** (login as SuperAdmin → Tenant Settings)
3. **Add employees** and departments
4. **Deploy to production** ([DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md))

---

## 💡 Quick Tips

- Default database is SQL Server LocalDB (automatically configured)
- API runs on port 5192 (HTTP) and 7082 (HTTPS)
- Frontend runs on port 5173
- All data is isolated by tenant (multi-tenant architecture)

---

**Need more help?** Check the [full documentation](./README/README.md)

**Ready to start?** Run the automated setup above! 🚀


