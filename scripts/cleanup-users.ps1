# =============================================
# Cleanup Users Script
# Deletes all users except SuperAdmin via API
# =============================================
# WARNING: This script will delete ALL users except SuperAdmin
# Make sure you have a backup before running this script
# =============================================

$apiUrl = "http://localhost:5000/api/dev/cleanup-users"

Write-Host "=============================================" -ForegroundColor Yellow
Write-Host "Cleanup Users Script" -ForegroundColor Yellow
Write-Host "This will delete ALL users except SuperAdmin" -ForegroundColor Yellow
Write-Host "=============================================" -ForegroundColor Yellow
Write-Host ""

# Confirm action
$confirmation = Read-Host "Are you sure you want to delete all users except SuperAdmin? (yes/no)"
if ($confirmation -ne "yes") {
    Write-Host "Operation cancelled." -ForegroundColor Red
    exit
}

Write-Host ""
Write-Host "Calling API endpoint..." -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post -ContentType "application/json"
    
    Write-Host ""
    Write-Host "✅ Success!" -ForegroundColor Green
    Write-Host "Message: $($response.message)" -ForegroundColor Green
    Write-Host "Deleted Count: $($response.deletedCount)" -ForegroundColor Green
    Write-Host "Total Users: $($response.totalUsers)" -ForegroundColor Green
    Write-Host "SuperAdmin Email: $($response.superAdminEmail)" -ForegroundColor Green
    
    if ($response.errors) {
        Write-Host ""
        Write-Host "⚠️ Errors occurred:" -ForegroundColor Yellow
        $response.errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    }
} catch {
    Write-Host ""
    Write-Host "❌ Error occurred:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        $errorDetails = $_.ErrorDetails.Message | ConvertFrom-Json
        Write-Host "Details: $($errorDetails.detail)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Script completed." -ForegroundColor Cyan

