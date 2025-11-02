# SmallHR First-Time Setup Script
# Run this once when setting up SmallHR on a new machine

$ErrorActionPreference = 'Stop'

Write-Host "🚀 SmallHR First-Time Setup" -ForegroundColor Green
Write-Host "=============================" -ForegroundColor Green
Write-Host ""

# Check if .NET 8 is installed
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ .NET SDK not found. Please install .NET 8 SDK from https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}
Write-Host "✅ .NET SDK found: $dotnetVersion" -ForegroundColor Green

# Check if Node.js is installed
Write-Host "Checking Node.js..." -ForegroundColor Yellow
$nodeVersion = node --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Node.js not found. Please install Node.js 20+ from https://nodejs.org" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Node.js found: $nodeVersion" -ForegroundColor Green

# Check if SQL Server LocalDB is accessible
Write-Host "Checking SQL Server LocalDB..." -ForegroundColor Yellow
try {
    sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT @@VERSION" -b 2>$null | Out-Null
    Write-Host "✅ SQL Server LocalDB is accessible" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Warning: SQL Server LocalDB may not be installed" -ForegroundColor Yellow
    Write-Host "   You can install it with Visual Studio or SQL Server Express" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Setting up backend..." -ForegroundColor Cyan
Set-Location "$PSScriptRoot\..\SmallHR.API"

# Initialize user secrets
Write-Host "   Initializing user secrets..." -ForegroundColor Yellow
dotnet user-secrets init 2>$null

# Set JWT secret (generate a random one)
$jwtSecret = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})
Write-Host "   Setting JWT secret..." -ForegroundColor Yellow
dotnet user-secrets set "Jwt:Key" $jwtSecret 2>$null

Write-Host "✅ Backend configured" -ForegroundColor Green

# Run database migrations
Write-Host ""
Write-Host "Setting up database..." -ForegroundColor Cyan
Set-Location "$PSScriptRoot\.."
Write-Host "   Running database migrations..." -ForegroundColor Yellow
dotnet ef database update --project SmallHR.Infrastructure --startup-project SmallHR.API

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Database setup complete" -ForegroundColor Green
} else {
    Write-Host "❌ Database migration failed" -ForegroundColor Red
    Write-Host "   Please check your SQL Server connection" -ForegroundColor Yellow
    exit 1
}

# Setup frontend
Write-Host ""
Write-Host "Setting up frontend..." -ForegroundColor Cyan
Set-Location "$PSScriptRoot\..\SmallHR.Web"

if (!(Test-Path "node_modules")) {
    Write-Host "   Installing npm packages (this may take a few minutes)..." -ForegroundColor Yellow
    npm install
} else {
    Write-Host "✅ Frontend dependencies already installed" -ForegroundColor Green
}

Write-Host ""
Write-Host "=============================" -ForegroundColor Green
Write-Host "✅ Setup Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "To start the application:" -ForegroundColor Cyan
Write-Host "  1. Open Terminal 1: cd SmallHR.API && dotnet run" -ForegroundColor White
Write-Host "  2. Open Terminal 2: cd SmallHR.Web && npm run dev" -ForegroundColor White
Write-Host ""
Write-Host "Then open: http://localhost:5173" -ForegroundColor Cyan
Write-Host ""
Write-Host "Default login:" -ForegroundColor Yellow
Write-Host "  Email: superadmin@smallhr.com" -ForegroundColor White
Write-Host "  Password: SuperAdmin@123" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  CHANGE THE PASSWORD AFTER FIRST LOGIN!" -ForegroundColor Red
Write-Host ""
Set-Location "$PSScriptRoot\.."


