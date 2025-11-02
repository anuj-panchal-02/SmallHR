# 🚀 SmallHR Deployment Guide

This guide covers three deployment scenarios:
1. **Development** (for testing on your machine)
2. **Production** (for real users/customers)
3. **Cloud Hosting** (Azure/AWS/DigitalOcean)

---

## 📋 Prerequisites

### Required Software
- **.NET 8 SDK** - Download from [Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 20+** - Download from [Node.js](https://nodejs.org/)
- **SQL Server** - Either:
  - SQL Server Express (free)
  - SQL Server Developer (free)
  - LocalDB (comes with Visual Studio)
  - SQL Server Azure/AWS RDS (cloud)

### For Production
- **Domain name** (optional but recommended)
- **SSL Certificate** (for HTTPS)
- **Web server** (IIS, Nginx, Apache)

---

## 🏠 Option 1: Development Deployment

**Use this for:** Testing on your local machine, demos, development

### Backend Setup

1. **Clone/Extract the Project**
   ```powershell
   cd C:\YourPath\smallHR
   ```

2. **Set JWT Secret**
   ```powershell
   cd SmallHR.API
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Key" "YourSecretKeyAtLeast32CharactersLong_ChangeThisInProduction123"
   ```

3. **Setup Database**
   ```powershell
   dotnet ef database update --project SmallHR.Infrastructure --startup-project SmallHR.API
   ```

4. **Run the API**
   ```powershell
   dotnet run --project SmallHR.API
   ```
   API will start at: `http://localhost:5192` (HTTP) or `https://localhost:7082` (HTTPS)

### Frontend Setup

1. **Install Dependencies**
   ```powershell
   cd SmallHR.Web
   npm install
   ```

2. **Update API URL** (if needed)
   Open `SmallHR.Web/src/services/api.ts` and verify the base URL is correct:
   ```typescript
   baseURL: 'https://localhost:7082' // or http://localhost:5192
   ```

3. **Run Frontend**
   ```powershell
   npm run dev
   ```
   Frontend will start at: `http://localhost:5173`

### Access the Application

1. Open browser: `http://localhost:5173`
2. Login with SuperAdmin credentials:
   - Email: `superadmin@smallhr.com`
   - Password: `SuperAdmin@123`

### Default Credentials

⚠️ **CHANGE THESE BEFORE DEPLOYING TO PRODUCTION!**

- **SuperAdmin**
  - Email: `superadmin@smallhr.com`
  - Password: `SuperAdmin@123`

---

## 🌐 Option 2: Production Deployment (Windows Server with IIS)

**Use this for:** Real customers, company internal use, dedicated server

### Server Requirements

- Windows Server 2016+ or Windows 10/11 Pro
- IIS 10+
- .NET 8 Runtime + Hosting Bundle
- SQL Server 2019+

### Backend Deployment

1. **Publish the API**
   ```powershell
   cd SmallHR.API
   dotnet publish -c Release -o C:\inetpub\wwwroot\smallhr-api
   ```

2. **Install .NET Hosting Bundle**
   Download and install: [ASP.NET Core 8.0 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)

3. **Configure IIS**
   - Open IIS Manager
   - Create new Application Pool: `SmallHRAPI`
     - .NET CLR Version: **No Managed Code**
     - Managed Pipeline Mode: **Integrated**
   - Create new Site: `SmallHRAPI`
     - Physical path: `C:\inetpub\wwwroot\smallhr-api`
     - Port: `80` or `443`
     - Bind Application Pool: `SmallHRAPI`

4. **Configure appsettings.json**
   Edit `C:\inetpub\wwwroot\smallhr-api\appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SQL_SERVER;Database=SmallHRDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=true;"
     },
     "Jwt": {
       "Key": "YOUR_STRONG_SECRET_KEY_AT_LEAST_32_CHARS_LONG"
     },
     "Cors": {
       "AllowedOrigins": [
         "https://yourdomain.com",
         "https://www.yourdomain.com"
       ]
     }
   }
   ```

5. **Configure Database**
   ```powershell
   cd SmallHR.API
   dotnet ef database update --project SmallHR.Infrastructure --startup-project SmallHR.API --connection "Server=YOUR_SQL_SERVER;Database=SmallHRDb;..."
   ```

6. **Set Permissions**
   ```powershell
   # Grant IIS_IUSRS full control
   icacls "C:\inetpub\wwwroot\smallhr-api" /grant "IIS_IUSRS:(OI)(CI)F"
   ```

### Frontend Deployment

1. **Build the Frontend**
   ```powershell
   cd SmallHR.Web
   npm install
   npm run build
   ```
   This creates optimized files in `SmallHR.Web/dist/`

2. **Configure API URL**
   Before building, edit `SmallHR.Web/src/services/api.ts`:
   ```typescript
   baseURL: 'https://yourdomain.com/api' // Your production API URL
   ```

3. **Deploy to IIS**
   - Create new Site: `SmallHRWeb`
   - Physical path: `C:\inetpub\wwwroot\smallhr-web` (copy dist contents here)
   - Port: `80` or `443`
   - Application Pool: Can use DefaultAppPool

4. **Configure Routing** (Important!)
   Create `web.config` in web root:
   ```xml
   <?xml version="1.0" encoding="UTF-8"?>
   <configuration>
     <system.webServer>
       <rewrite>
         <rules>
           <rule name="React Routes" stopProcessing="true">
             <match url=".*" />
             <conditions logicalGrouping="MatchAll">
               <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
               <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
             </conditions>
             <action type="Rewrite" url="/index.html" />
           </rule>
         </rules>
       </rewrite>
     </system.webServer>
   </configuration>
   ```

### Enable HTTPS

1. **Request SSL Certificate** from your domain provider or use Let's Encrypt

2. **Install Certificate** in IIS:
   - Server Certificates → Import
   - Bind HTTPS to your sites

3. **Force HTTPS** in API's `Program.cs` (already configured)

---

## ☁️ Option 3: Cloud Deployment (Azure/AWS)

**Use this for:** Scalable, managed cloud hosting

### Azure Deployment

#### Backend (Azure App Service)

1. **Create App Service**
   ```bash
   az webapp create --resource-group smallhr-rg --plan smallhr-plan --name smallhr-api --runtime "DOTNET|8.0"
   ```

2. **Configure Connection String**
   ```bash
   az webapp config connection-string set \
     --resource-group smallhr-rg \
     --name smallhr-api \
     --connection-string-type SQLServer \
     --settings DefaultConnection="Server=your-server.database.windows.net;Database=SmallHRDb;..."
   ```

3. **Configure App Settings**
   ```bash
   az webapp config appsettings set \
     --resource-group smallhr-rg \
     --name smallhr-api \
     --settings Jwt__Key="YOUR_SECRET_KEY"
   ```

4. **Deploy**
   ```bash
   cd SmallHR.API
   az webapp up --resource-group smallhr-rg --name smallhr-api
   ```

#### Database (Azure SQL Database)

1. **Create SQL Database**
   ```bash
   az sql db create \
     --resource-group smallhr-rg \
     --server smallhr-server \
     --name SmallHRDb \
     --service-objective S0
   ```

2. **Run Migrations**
   ```powershell
   dotnet ef database update --project SmallHR.Infrastructure --startup-project SmallHR.API
   ```

#### Frontend (Azure Static Web Apps or Blob Storage)

1. **Build Frontend**
   ```powershell
   cd SmallHR.Web
   npm run build
   ```

2. **Deploy to Static Web Apps**
   ```bash
   az staticwebapp create --name smallhr-web --resource-group smallhr-rg --source ./SmallHR.Web/dist
   ```

### AWS Deployment

#### Backend (AWS Elastic Beanstalk or ECS)

1. **Create Beanstalk Environment**
   - Upload `.NET Core on Linux` platform
   - Upload your `SmallHR.API.zip`

2. **Configure RDS Database**
   - Create SQL Server RDS instance
   - Update connection string in appsettings

3. **Configure Environment Variables**
   - Add `Jwt__Key` in Beanstalk Configuration

#### Frontend (AWS S3 + CloudFront)

1. **Upload to S3**
   ```bash
   aws s3 sync SmallHR.Web/dist s3://your-bucket-name
   ```

2. **Configure CloudFront**
   - Create distribution pointing to S3
   - Enable HTTPS

---

## 🔐 Security Checklist (CRITICAL!)

Before going live, ensure:

- [ ] Changed default SuperAdmin password
- [ ] Set strong JWT secret (32+ characters, random)
- [ ] Configured production database connection string
- [ ] Enabled HTTPS (required for production)
- [ ] Updated CORS settings with your domain
- [ ] Configured firewall rules
- [ ] Set up database backups
- [ ] Configured logging
- [ ] Set proper file permissions
- [ ] Disabled debug logging in production
- [ ] Reviewed [Security Audit Report](./README/SECURITY_AUDIT_REPORT.md)

---

## 📦 Quick Deployment Scripts

### Windows PowerShell Script

Create `deploy-production.ps1`:

```powershell
# Deploy SmallHR to Production
param(
    [string]$ApiPath = "C:\inetpub\wwwroot\smallhr-api",
    [string]$WebPath = "C:\inetpub\wwwroot\smallhr-web"
)

Write-Host "🚀 Starting SmallHR Production Deployment..." -ForegroundColor Green

# Stop services
Write-Host "Stopping IIS..." -ForegroundColor Yellow
Stop-Service W3SVC -ErrorAction SilentlyContinue

# Backup existing deployment
Write-Host "Creating backup..." -ForegroundColor Yellow
Copy-Item $ApiPath "$ApiPath-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')" -Recurse -Force

# Publish API
Write-Host "Publishing API..." -ForegroundColor Yellow
dotnet publish SmallHR.API\SmallHR.API.csproj -c Release -o $ApiPath

# Build Frontend
Write-Host "Building frontend..." -ForegroundColor Yellow
cd SmallHR.Web
npm run build

# Deploy Frontend
Write-Host "Deploying frontend..." -ForegroundColor Yellow
Copy-Item dist\* $WebPath -Recurse -Force

# Restart services
Write-Host "Starting IIS..." -ForegroundColor Yellow
Start-Service W3SVC

Write-Host "✅ Deployment Complete!" -ForegroundColor Green
Write-Host "API: https://yourdomain.com" -ForegroundColor Cyan
Write-Host "Web: https://yourdomain.com" -ForegroundColor Cyan
```

Run with: `.\deploy-production.ps1`

---

## 📊 Monitoring & Maintenance

### Recommended Tools

- **Application Insights** (Azure)
- **CloudWatch** (AWS)
- **ELK Stack** (Self-hosted)
- **New Relic** (Third-party)

### Logging

Logs are configured in `appsettings.json`. For production:
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

### Database Backups

Set up automated backups:
- Daily full backups
- Hourly transaction log backups
- 30-day retention minimum

---

## 🆘 Troubleshooting

### API Won't Start

1. Check logs: `Event Viewer → Windows Logs → Application`
2. Verify connection string
3. Check database is accessible
4. Verify .NET 8 Hosting Bundle is installed

### Frontend Shows Blank Page

1. Check browser console (F12)
2. Verify API URL in `services/api.ts`
3. Check CORS settings
4. Verify `web.config` routing is configured

### Database Issues

1. Run migrations manually:
   ```powershell
   dotnet ef database update --project SmallHR.Infrastructure --startup-project SmallHR.API
   ```

2. Check connection string
3. Verify SQL Server is running

See **[API_TROUBLESHOOTING.md](./README/API_TROUBLESHOOTING.md)** for more details.

---

## 📞 Support

For issues or questions:
1. Check [Documentation](./README/README.md)
2. Review [API Troubleshooting Guide](./README/API_TROUBLESHOOTING.md)
3. Check [Security Audit Report](./README/SECURITY_AUDIT_REPORT.md)

---

## 🎉 Next Steps After Deployment

1. **Create First Tenant**
   - Login as SuperAdmin
   - Go to Tenant Settings
   - Create your first company tenant

2. **Configure Users**
   - Create employee accounts
   - Assign roles (Admin, HR, Employee)

3. **Setup Departments & Positions**
   - Add company departments
   - Define job positions

4. **Import Data** (optional)
   - Bulk import employees via CSV (if feature exists)
   - Import leave policies

---

**Last Updated:** 2025-01-28

