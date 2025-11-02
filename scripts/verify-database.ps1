# Verify Database Configuration
# This script verifies that migrations are applied to SmallHRDb (primary database)

$connectionString = "Server=(localdb)\mssqllocaldb;Trusted_Connection=true;TrustServerCertificate=true"

Write-Host "Verifying Database Configuration" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Check SmallHRDb
Write-Host "Checking SmallHRDb..." -ForegroundColor Yellow
try {
    $query = "SELECT COUNT(*) AS MigrationCount FROM __EFMigrationsHistory"
    $result = Invoke-Sqlcmd -ConnectionString "$connectionString;Database=SmallHRDb" -Query $query -ErrorAction Stop
    Write-Host "  SmallHRDb: $($result.MigrationCount) migrations applied" -ForegroundColor Green
    
    # List migrations
    $migrationsQuery = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId"
    $migrations = Invoke-Sqlcmd -ConnectionString "$connectionString;Database=SmallHRDb" -Query $migrationsQuery -ErrorAction Stop
    Write-Host "  Latest migration: $($migrations[-1].MigrationId)" -ForegroundColor Gray
    
    # Check AdminAudits table
    $tableQuery = "SELECT COUNT(*) AS TableCount FROM sys.tables WHERE name = 'AdminAudits'"
    $tableResult = Invoke-Sqlcmd -ConnectionString "$connectionString;Database=SmallHRDb" -Query $tableQuery -ErrorAction Stop
    if ($tableResult.TableCount -gt 0) {
        Write-Host "  AdminAudits table: EXISTS" -ForegroundColor Green
    } else {
        Write-Host "  AdminAudits table: NOT FOUND" -ForegroundColor Red
    }
}
catch {
    Write-Host "  SmallHRDb: ERROR - $_" -ForegroundColor Red
}

Write-Host ""

# Note: Only SmallHRDb should be used
Write-Host "Note: Only SmallHRDb should be used for all migrations and data." -ForegroundColor Cyan

Write-Host ""
Write-Host "Verification Complete!" -ForegroundColor Cyan

