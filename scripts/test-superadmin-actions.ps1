# Test SuperAdmin Actions on Admin Endpoints
# This script tests SuperAdmin access to admin endpoints and verifies audit logging

param(
    [string]$BaseUrl = "http://localhost:5192",
    [string]$SuperAdminEmail = "superadmin@smallhr.com",
    [string]$SuperAdminPassword = "SuperAdmin@123"
)

Write-Host "Testing SuperAdmin Actions on Admin Endpoints" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Login as SuperAdmin
Write-Host "Step 1: Logging in as SuperAdmin..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = $SuperAdminEmail
        password = $SuperAdminPassword
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop

    $token = $loginResponse.token
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }

    Write-Host " Login successful" -ForegroundColor Green
    Write-Host "  Token received: $($token.Substring(0, 20))..." -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host " Login failed: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Test User Management Endpoint (Admin Endpoint)
Write-Host "Step 2: Testing User Management Endpoint (/api/usermanagement/users)..." -ForegroundColor Yellow
try {
    $usersResponse = Invoke-RestMethod -Uri "$BaseUrl/api/usermanagement/users" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host " User Management endpoint accessed successfully" -ForegroundColor Green
    Write-Host "  Users returned: $($usersResponse.Count)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host " User Management endpoint failed: $_" -ForegroundColor Red
    Write-Host "  Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
}

# Step 3: Test Admin Controller Endpoint
Write-Host "Step 3: Testing Admin Controller Endpoint (/api/admin/verify-superadmin)..." -ForegroundColor Yellow
try {
    $verifyResponse = Invoke-RestMethod -Uri "$BaseUrl/api/admin/verify-superadmin" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host " Admin verify endpoint accessed successfully" -ForegroundColor Green
    Write-Host "  SuperAdmin Configuration Valid: $($verifyResponse.isValid)" -ForegroundColor Gray
    Write-Host "  Total SuperAdmins: $($verifyResponse.totalSuperAdmins)" -ForegroundColor Gray
    Write-Host "  Needs Fix: $($verifyResponse.needsFix)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host " Admin verify endpoint failed: $_" -ForegroundColor Red
}

# Step 4: Test Tenants Endpoint (Admin Endpoint)
Write-Host "Step 4: Testing Tenants Endpoint (/api/tenants)..." -ForegroundColor Yellow
try {
    $tenantsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/tenants" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host " Tenants endpoint accessed successfully" -ForegroundColor Green
    Write-Host "  Tenants returned: $($tenantsResponse.Count)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host " Tenants endpoint failed: $_" -ForegroundColor Red
}

# Step 5: Check Audit Logs
Write-Host "Step 5: Checking Audit Logs (/api/adminaudit)..." -ForegroundColor Yellow
try {
    $auditResponse = Invoke-RestMethod -Uri "$BaseUrl/api/adminaudit?pageSize=10" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host " Audit logs retrieved successfully" -ForegroundColor Green
    Write-Host "  Total Audit Logs: $($auditResponse.totalCount)" -ForegroundColor Gray
    Write-Host "  Audit Logs Returned: $($auditResponse.auditLogs.Count)" -ForegroundColor Gray
    Write-Host ""
    
    if ($auditResponse.auditLogs.Count -gt 0) {
        Write-Host "  Recent Audit Logs:" -ForegroundColor Cyan
        foreach ($log in $auditResponse.auditLogs | Select-Object -First 5) {
            $logInfo = "    - $($log.actionType) - $($log.httpMethod) $($log.endpoint) - Status: $($log.statusCode) - Success: $($log.isSuccess)"
            Write-Host $logInfo -ForegroundColor Gray
        }
    }
    Write-Host ""
}
catch {
    Write-Host " Audit logs check failed: $_" -ForegroundColor Red
}

# Step 6: Test Regular Endpoint (should NOT bypass query filters)
Write-Host "Step 6: Testing Regular Endpoint (/api/employees) - Query filters should NOT be bypassed..." -ForegroundColor Yellow
try {
    $employeesResponse = Invoke-RestMethod -Uri "$BaseUrl/api/employees" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host " Employees endpoint accessed (may be empty if no employees for platform tenant)" -ForegroundColor Green
    Write-Host "  Employees returned: $($employeesResponse.Count)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host " Employees endpoint failed: $_" -ForegroundColor Red
}

# Step 7: Test Audit Statistics
Write-Host "Step 7: Testing Audit Statistics (/api/adminaudit/statistics)..." -ForegroundColor Yellow
try {
    $statsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/adminaudit/statistics" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host " Audit statistics retrieved successfully" -ForegroundColor Green
    Write-Host "  Total Actions: $($statsResponse.totalActions)" -ForegroundColor Gray
    Write-Host "  Successful Actions: $($statsResponse.successfulActions)" -ForegroundColor Gray
    Write-Host "  Failed Actions: $($statsResponse.failedActions)" -ForegroundColor Gray
    Write-Host "  Success Rate: $([math]::Round($statsResponse.successRate, 2))%" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host " Audit statistics failed: $_" -ForegroundColor Red
}

Write-Host "Test Complete!" -ForegroundColor Cyan
Write-Host "=================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "- SuperAdmin should be able to access admin endpoints" -ForegroundColor White
Write-Host "- All SuperAdmin actions should be logged in AdminAudit table" -ForegroundColor White
Write-Host "- Query filters should be bypassed only on admin endpoints" -ForegroundColor White
Write-Host "- Regular endpoints should still respect tenant isolation" -ForegroundColor White

