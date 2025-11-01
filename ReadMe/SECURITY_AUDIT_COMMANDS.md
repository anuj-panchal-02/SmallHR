# 🔍 Local Security Audit Commands

This document provides CLI commands to run security audits locally on the SmallHR codebase.

---

## Prerequisites

```powershell
# Install .NET 8 SDK
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0

# Install Node.js 20+
# Download from: https://nodejs.org/

# Install Python 3.10+ (for detect-secrets)
# Download from: https://www.python.org/downloads/
```

---

## 1. Static Code Analysis

### DotNet Format Check

```powershell
# Check formatting
dotnet format --verify --verbosity diagnostic SmallHR.API/SmallHR.API.csproj
dotnet format --verify --verbosity diagnostic SmallHR.Infrastructure/SmallHR.Infrastructure.csproj
dotnet format --verify --verbosity diagnostic SmallHR.Core/SmallHR.Core.csproj

# Fix formatting
dotnet format SmallHR.API/SmallHR.API.csproj
dotnet format SmallHR.Infrastructure/SmallHR.Infrastructure.csproj
dotnet format SmallHR.Core/SmallHR.Core.csproj
```

### Roslyn Analyzers

```powershell
# Build with analyzers (warnings as errors)
dotnet build --configuration Release /p:TreatWarningsAsErrors=true

# List all analyzer warnings
dotnet build --configuration Release 2>&1 | Select-String "warning"
```

### SecurityCodeScan (Roslyn Analyzer)

```powershell
# Install SecurityCodeScan
dotnet add SmallHR.API/SmallHR.API.csproj package SecurityCodeScan.VS2019

# Build to see security warnings
dotnet build --configuration Release
```

---

## 2. Dependency & SCA Scanning

### NuGet Vulnerable Packages

```powershell
# List vulnerable packages
dotnet list package --vulnerable --include-transitive

# Output to JSON
dotnet list package --vulnerable --include-transitive --format json > vulnerable-packages.json
```

### NPM Audit

```powershell
# Navigate to frontend
cd SmallHR.Web

# Run npm audit
npm audit

# Run npm audit with JSON output
npm audit --json > npm-audit-report.json

# Fix vulnerabilities automatically (where possible)
npm audit fix

# Fix vulnerabilities without updating package.json
npm audit fix --force
```

### OWASP Dependency-Check

```powershell
# Download OWASP Dependency-Check
# From: https://github.com/jeremylong/DependencyCheck/releases

# Run scan for .NET projects
.\dependency-check\bin\dependency-check.bat --scan SmallHR.API --scan SmallHR.Infrastructure --scan SmallHR.Core --format HTML --format JSON --out reports\dotnet-dependency-check --enableExperimental

# Check report
Start-Process reports\dotnet-dependency-check\dependency-check-report.html
```

---

## 3. Secret Scanning

### Git-Secrets (Git Hook)

```powershell
# Install git-secrets (requires Git Bash or WSL)
# Windows: Use Git Bash or WSL
# From: https://github.com/awslabs/git-secrets

# Install git-secrets
git clone https://github.com/awslabs/git-secrets.git
cd git-secrets
make install

# Add to repo
cd C:\Users\Anuj\Desktop\smallHR
git secrets --install
git secrets --register-aws

# Scan existing commits
git secrets --scan-history
```

### TruffleHog

```powershell
# Install TruffleHog (via pip)
pip install truffleHog

# Scan repository
truffleHog --regex --entropy=True --json . > trufflehog-report.json

# Scan specific branch
truffleHog git file://. --since_commit HEAD~10 --json > trufflehog-report.json
```

### Detect-Secrets

```powershell
# Install detect-secrets
pip install detect-secrets

# Scan for secrets
detect-secrets scan --baseline .secrets.baseline

# Audit baseline
detect-secrets audit .secrets.baseline

# Scan specific files
detect-secrets scan --all-files
```

### Gitleaks

```powershell
# Download Gitleaks
# From: https://github.com/gitleaks/gitleaks/releases

# Scan repository
.\gitleaks.exe detect --source . --report-path gitleaks-report.json

# Scan with verbose output
.\gitleaks.exe detect --source . --verbose --report-path gitleaks-report.json
```

---

## 4. Code Duplication & Complexity

### CPD (Copy-Paste Detector)

```powershell
# Install PMD CPD (requires Java)
# Download PMD from: https://pmd.github.io/pmd/pmd_download_latest.html

# Run CPD scan
java -cp .\pmd-bin\lib\* net.sourceforge.pmd.cpd.CPD --minimum-tokens 100 --language cs --files SmallHR.API --files SmallHR.Infrastructure --files SmallHR.Core --format csv > cpd-report.csv
```

### Roslynator

```powershell
# Install Roslynator
dotnet add SmallHR.API/SmallHR.API.csproj package Roslynator.Analyzers

# Build to see duplication warnings
dotnet build --configuration Release
```

---

## 5. Container Security (If Using Docker)

### Trivy Scan

```powershell
# Install Trivy
# Download from: https://github.com/aquasecurity/trivy/releases

# Scan Dockerfile
trivy fs --severity HIGH,CRITICAL .

# Scan Docker image
trivy image --severity HIGH,CRITICAL your-image:tag
```

---

## 6. Multi-Tenant Isolation Testing

### Run Security Tests

```powershell
# Run security-focused tests
dotnet test --filter "Category=Security" --verbosity normal

# Run tenant isolation tests
dotnet test --filter "Category=TenantIsolation" --verbosity normal
```

---

## 7. Input Validation Testing

### SQL Injection Test

```powershell
# Run SQL injection tests
dotnet test --filter "Category=SqlInjection" --verbosity normal
```

---

## 8. Performance & Load Testing

### Load Testing (NBomber)

```powershell
# Install NBomber
dotnet add SmallHR.Tests/SmallHR.Tests.csproj package NBomber

# Run load tests
dotnet test --filter "Category=LoadTest" --verbosity normal
```

---

## 9. Code Coverage

### Generate Coverage Report

```powershell
# Install coverlet
dotnet add SmallHR.Tests/SmallHR.Tests.csproj package coverlet.msbuild

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory:"coverage-results"

# Generate report (requires ReportGenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"coverage-results/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:"Html"
```

---

## 10. Complete Audit Script

### PowerShell Script: `run-security-audit.ps1`

```powershell
# Complete Security Audit Script
Write-Host "Starting Security Audit..." -ForegroundColor Cyan

# 1. Static Analysis
Write-Host "`n1. Running Code Format Check..." -ForegroundColor Yellow
dotnet format --verify --verbosity diagnostic 2>&1 | Out-File -FilePath "audit-results\format-check.txt"

# 2. Build with Analyzers
Write-Host "`n2. Building with Analyzers..." -ForegroundColor Yellow
dotnet build --configuration Release /p:TreatWarningsAsErrors=false 2>&1 | Out-File -FilePath "audit-results\analyzer-warnings.txt"

# 3. Vulnerable Packages
Write-Host "`n3. Checking Vulnerable Packages..." -ForegroundColor Yellow
dotnet list package --vulnerable --include-transitive --format json | Out-File -FilePath "audit-results\vulnerable-packages.json"

# 4. NPM Audit
Write-Host "`n4. Running NPM Audit..." -ForegroundColor Yellow
Set-Location SmallHR.Web
npm audit --json | Out-File -FilePath "..\audit-results\npm-audit-report.json"
Set-Location ..

# 5. Run Tests
Write-Host "`n5. Running Security Tests..." -ForegroundColor Yellow
dotnet test --filter "Category=Security" --verbosity normal --logger "trx;LogFileName=audit-results\security-tests.trx"

# 6. Code Coverage
Write-Host "`n6. Generating Code Coverage..." -ForegroundColor Yellow
dotnet test --collect:"XPlat Code Coverage" --results-directory:"audit-results\coverage"

Write-Host "`n✅ Security Audit Complete!" -ForegroundColor Green
Write-Host "Results saved to: audit-results\" -ForegroundColor Green
```

---

## Summary

Run these commands regularly (weekly) or before each release:

1. **Format Check:** `dotnet format --verify`
2. **Build with Analyzers:** `dotnet build --configuration Release`
3. **Vulnerable Packages:** `dotnet list package --vulnerable`
4. **NPM Audit:** `npm audit`
5. **Secret Scan:** `gitleaks detect --source .`
6. **Security Tests:** `dotnet test --filter "Category=Security"`

**Estimated Time:** ~30 minutes for full audit

