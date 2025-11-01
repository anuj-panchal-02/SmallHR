# 🔐 Quick Start: JWT Secret Setup

This guide shows you exactly how to ensure `JWT_SECRET_KEY` is set before running the application.

---

## ✅ Method 1: Check if Already Set

First, check if the secret is already configured:

```powershell
cd SmallHR.API
dotnet user-secrets list
```

**If you see `Jwt:Key` in the list, you're all set! ✅**

---

## 🔧 Method 2: Set Using dotnet user-secrets (Recommended)

**This is the recommended method for development:**

```powershell
cd SmallHR.API

# Initialize user-secrets (only needed once)
dotnet user-secrets init

# Set the JWT secret
dotnet user-secrets set "Jwt:Key" "SmallHR_SuperSecure_JWT_SecretKey_2025_AtLeast32CharactersLong_Required"
```

**Verify it's set:**
```powershell
dotnet user-secrets list
```

**You should see:**
```
Jwt:Key = SmallHR_SuperSecure_JWT_SecretKey_2025_AtLeast32CharactersLong_Required
```

---

## 🔧 Method 3: Set Using Environment Variable (Current Session)

**For the current PowerShell session only:**

```powershell
$env:JWT_SECRET_KEY = "SmallHR_SuperSecure_JWT_SecretKey_2025_AtLeast32CharactersLong_Required"
```

**Verify it's set:**
```powershell
Write-Host $env:JWT_SECRET_KEY
```

**⚠️ Note:** This only lasts for the current terminal session. Closing the terminal will require setting it again.

---

## 🔧 Method 4: Set Using Environment Variable (Permanent)

**For Windows (User-level - persists across sessions):**

```powershell
[System.Environment]::SetEnvironmentVariable("JWT_SECRET_KEY", "SmallHR_SuperSecure_JWT_SecretKey_2025_AtLeast32CharactersLong_Required", "User")
```

**For Windows (System-level - requires Admin):**

```powershell
[System.Environment]::SetEnvironmentVariable("JWT_SECRET_KEY", "SmallHR_SuperSecure_JWT_SecretKey_2025_AtLeast32CharactersLong_Required", "Machine")
```

**Restart terminal after setting permanently.**

**Verify it's set:**
```powershell
# Close and reopen terminal, then:
Write-Host $env:JWT_SECRET_KEY
```

---

## 🚀 Method 5: Quick Setup Script

**Use the setup script:**

```powershell
.\scripts\set-jwt-secret.ps1
```

This will:
- Set it for the current session
- Show you how to set it permanently
- Provide instructions for dotnet user-secrets

---

## ✅ Verification Checklist

Before running the application, verify:

1. **Check user-secrets:**
   ```powershell
   cd SmallHR.API
   dotnet user-secrets list
   ```
   ✅ Should show `Jwt:Key`

2. **OR check environment variable:**
   ```powershell
   Write-Host $env:JWT_SECRET_KEY
   ```
   ✅ Should show your secret key (or empty if not set)

3. **Run the application:**
   ```powershell
   dotnet run --project SmallHR.API
   ```
   ✅ Should start without JWT secret errors

---

## ⚠️ What Happens If Not Set?

If `JWT_SECRET_KEY` is not set, the application will fail to start with this error:

```
System.InvalidOperationException: JWT_SECRET_KEY must be set in environment variables or appsettings.json
```

**Solution:** Use one of the methods above to set it.

---

## 📋 Quick Reference

| Method | Persistence | Recommended For |
|--------|-------------|----------------|
| **dotnet user-secrets** | ✅ Persistent | Development |
| **Environment Variable (Session)** | ❌ Current session only | Testing |
| **Environment Variable (User)** | ✅ Persistent | Development |
| **Environment Variable (System)** | ✅ Persistent | Production |

---

## 🎯 Recommended Setup for Development

**Use dotnet user-secrets (Method 2):**

1. Open PowerShell
2. Navigate to project: `cd SmallHR.API`
3. Initialize: `dotnet user-secrets init`
4. Set secret: `dotnet user-secrets set "Jwt:Key" "YourSecretKeyHere"`
5. Verify: `dotnet user-secrets list`

**That's it!** The secret will persist and work every time you run the application.

---

## 🔐 Generate a Secure Secret

**PowerShell (32 bytes, base64):**
```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($bytes)
$secret = [Convert]::ToBase64String($bytes)
Write-Host $secret
```

**Or use this secure random key:**
```
SmallHR_SuperSecure_JWT_SecretKey_2025_AtLeast32CharactersLong_Required
```

---

**Last Updated:** 2025-01-27

