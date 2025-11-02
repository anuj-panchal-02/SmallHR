# =============================================
# Clean Database Script
# Removes all data except roles and creates 1 SuperAdmin
# =============================================
# WARNING: This script will delete ALL data except roles
# and create only 1 SuperAdmin user
# =============================================

param(
    [string]$ApiUrl = "http://localhost:5192"
)

Write-Host "=============================================" -ForegroundColor Yellow
Write-Host "Clean Database Script" -ForegroundColor Yellow
Write-Host "This will:" -ForegroundColor Yellow
Write-Host "  - Delete ALL users except SuperAdmin" -ForegroundColor Yellow
Write-Host "  - Delete ALL tenants and related data:" -ForegroundColor Yellow
Write-Host "    * Alerts, WebhookEvents, AdminAudits" -ForegroundColor Yellow
Write-Host "    * TenantLifecycleEvents, TenantUsageMetrics" -ForegroundColor Yellow
Write-Host "    * SubscriptionPlanFeatures, Subscriptions" -ForegroundColor Yellow
Write-Host "    * RolePermissions, Modules" -ForegroundColor Yellow
Write-Host "    * Positions, Departments" -ForegroundColor Yellow
Write-Host "    * Attendances, LeaveRequests, Employees" -ForegroundColor Yellow
Write-Host "    * Tenants" -ForegroundColor Yellow
Write-Host "  - Create 1 SuperAdmin user (superadmin@smallhr.com)" -ForegroundColor Yellow
Write-Host "  - Preserve all roles (SuperAdmin, Admin, HR, Employee)" -ForegroundColor Yellow
Write-Host "=============================================" -ForegroundColor Yellow
Write-Host ""

# Confirm action
$confirmation = Read-Host "Are you sure you want to clean the database? (yes/no)"
if ($confirmation -ne "yes") {
    Write-Host "Operation cancelled." -ForegroundColor Red
    exit
}

Write-Host ""
Write-Host "Calling API endpoint to clean database..." -ForegroundColor Cyan

try {
    # Use clean-all-data endpoint (keeps database, removes all data)
    $response = Invoke-RestMethod -Uri "$ApiUrl/api/dev/clean-all-data" `
        -Method Post `
        -ContentType "application/json" `
        -ErrorAction Stop
    
    Write-Host ""
    Write-Host "Success!" -ForegroundColor Green
    Write-Host "Message: $($response.message)" -ForegroundColor Green
    Write-Host ""
    Write-Host "Database has been cleaned and reset with:" -ForegroundColor Cyan
    Write-Host "  ✅ All essential roles (SuperAdmin, Admin, HR, Employee)" -ForegroundColor White
    Write-Host "  ✅ 1 SuperAdmin user: superadmin@smallhr.com" -ForegroundColor White
    Write-Host "  ✅ Password: SuperAdmin@123" -ForegroundColor White
    Write-Host ""
    Write-Host "All tenant-related data has been removed:" -ForegroundColor Cyan
    Write-Host "  ✅ Alerts, WebhookEvents, AdminAudits" -ForegroundColor White
    Write-Host "  ✅ TenantLifecycleEvents, TenantUsageMetrics" -ForegroundColor White
    Write-Host "  ✅ SubscriptionPlanFeatures, Subscriptions" -ForegroundColor White
    Write-Host "  ✅ RolePermissions, Modules" -ForegroundColor White
    Write-Host "  ✅ Positions, Departments" -ForegroundColor White
    Write-Host "  ✅ Attendances, LeaveRequests, Employees" -ForegroundColor White
    Write-Host "  ✅ Tenants" -ForegroundColor White
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "Error occurred:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        try {
            $errorDetails = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host "Details: $($errorDetails.detail)" -ForegroundColor Red
        } catch {
            Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    Write-Host "Make sure the API is running at: $ApiUrl" -ForegroundColor Yellow
    Write-Host "Run: cd SmallHR.API && dotnet run" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Script completed." -ForegroundColor Cyan
