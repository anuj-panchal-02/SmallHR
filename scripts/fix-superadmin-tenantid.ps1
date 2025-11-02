# Fix SuperAdmin Users: Set TenantId = NULL
# SuperAdmin users operate at the platform layer, not tenant layer
# This script updates all SuperAdmin users to have TenantId = NULL

param(
    [string]$ConnectionString = "Server=localhost;Database=SmallHR;Integrated Security=true;TrustServerCertificate=true;"
)

Write-Host "Fixing SuperAdmin users to have TenantId = NULL..." -ForegroundColor Cyan

try {
    # Load SQL Server module if available
    Import-Module SqlServer -ErrorAction SilentlyContinue
    
    # Execute SQL query to update SuperAdmin users
    $query = @"
-- Update all users with SuperAdmin role to have TenantId = NULL
UPDATE [AspNetUsers]
SET [TenantId] = NULL
WHERE [Id] IN (
    SELECT ur.[UserId]
    FROM [AspNetUserRoles] ur
    INNER JOIN [AspNetRoles] r ON ur.[RoleId] = r.[Id]
    WHERE r.[Name] = 'SuperAdmin'
)
AND [TenantId] IS NOT NULL;

-- Verify the update
SELECT 
    u.[Email],
    u.[FirstName],
    u.[LastName],
    u.[TenantId],
    r.[Name] AS [Role]
FROM [AspNetUsers] u
INNER JOIN [AspNetUserRoles] ur ON u.[Id] = ur.[UserId]
INNER JOIN [AspNetRoles] r ON ur.[RoleId] = r.[Id]
WHERE r.[Name] = 'SuperAdmin';
"@
    
    Write-Host "Executing SQL query..." -ForegroundColor Yellow
    $results = Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $query
    
    if ($results) {
        Write-Host "`nSuperAdmin users after update:" -ForegroundColor Green
        $results | Format-Table -AutoSize
        
        $updatedCount = ($results | Where-Object { $_.TenantId -eq $null }).Count
        Write-Host "`nUpdated $updatedCount SuperAdmin user(s) to have TenantId = NULL" -ForegroundColor Green
    } else {
        Write-Host "No SuperAdmin users found or no updates needed." -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host "`nTrying alternative method using .NET SQL Client..." -ForegroundColor Yellow
    
    # Alternative method using .NET
    try {
        Add-Type -Path "C:\Program Files\dotnet\shared\Microsoft.NETCore.App\*\System.Data.SqlClient.dll" -ErrorAction SilentlyContinue
        
        $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $connection.Open()
        
        $updateCommand = $connection.CreateCommand()
        $updateCommand.CommandText = @"
UPDATE [AspNetUsers]
SET [TenantId] = NULL
WHERE [Id] IN (
    SELECT ur.[UserId]
    FROM [AspNetUserRoles] ur
    INNER JOIN [AspNetRoles] r ON ur.[RoleId] = r.[Id]
    WHERE r.[Name] = 'SuperAdmin'
)
AND [TenantId] IS NOT NULL;
"@
        $rowsAffected = $updateCommand.ExecuteNonQuery()
        
        Write-Host "Updated $rowsAffected SuperAdmin user(s) to have TenantId = NULL" -ForegroundColor Green
        
        # Verify
        $verifyCommand = $connection.CreateCommand()
        $verifyCommand.CommandText = @"
SELECT 
    u.[Email],
    u.[FirstName],
    u.[LastName],
    u.[TenantId],
    r.[Name] AS [Role]
FROM [AspNetUsers] u
INNER JOIN [AspNetUserRoles] ur ON u.[Id] = ur.[UserId]
INNER JOIN [AspNetRoles] r ON ur.[RoleId] = r.[Id]
WHERE r.[Name] = 'SuperAdmin';
"@
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($verifyCommand)
        $dataset = New-Object System.Data.DataSet
        $adapter.Fill($dataset)
        
        if ($dataset.Tables[0].Rows.Count -gt 0) {
            Write-Host "`nSuperAdmin users after update:" -ForegroundColor Green
            $dataset.Tables[0] | Format-Table -AutoSize
        }
        
        $connection.Close()
    }
    catch {
        Write-Host "Alternative method also failed: $_" -ForegroundColor Red
        Write-Host "`nPlease run the SQL script manually: scripts/fix-superadmin-tenantid.sql" -ForegroundColor Yellow
    }
}

Write-Host "`nDone!" -ForegroundColor Cyan

