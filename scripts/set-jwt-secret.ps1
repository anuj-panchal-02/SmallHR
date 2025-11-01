# Set JWT Secret for Development
# This script sets the JWT_SECRET_KEY environment variable

Write-Host "Setting JWT_SECRET_KEY environment variable..." -ForegroundColor Cyan

# Generate a secure random key (32+ characters)
$jwtSecret = "SmallHR_SuperSecure_JWT_SecretKey_2025_AtLeast32CharactersLong_Required"

# Set for current session
$env:JWT_SECRET_KEY = $jwtSecret

Write-Host "✅ JWT_SECRET_KEY set for current PowerShell session" -ForegroundColor Green
Write-Host "   Value: $jwtSecret" -ForegroundColor Gray
Write-Host ""
Write-Host "⚠️  Note: This is only set for the current session." -ForegroundColor Yellow
Write-Host "   To make it persistent, choose one of the options below:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Option 1: Use dotnet user-secrets (Recommended for development):" -ForegroundColor Cyan
Write-Host "   dotnet user-secrets set `"Jwt:Key`" `"$jwtSecret`"" -ForegroundColor White
Write-Host ""
Write-Host "Option 2: Set system environment variable:" -ForegroundColor Cyan
Write-Host "   [System.Environment]::SetEnvironmentVariable(`"JWT_SECRET_KEY`", `"$jwtSecret`", `"User`")" -ForegroundColor White
Write-Host ""
Write-Host "Option 3: Add to appsettings.Development.json (Less secure, not recommended):" -ForegroundColor Cyan
Write-Host "   Add: `"Jwt: { `"Key`": `"$jwtSecret`" }`"" -ForegroundColor White

