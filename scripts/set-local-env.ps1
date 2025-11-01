$ErrorActionPreference = 'Stop'

Write-Host 'Configuring environment variables for SmallHR (local)...'

# Connection string (adjust as needed)
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\mssqllocaldb;Database=SmallHRDb;Trusted_Connection=true;MultipleActiveResultSets=true"

# JWT settings (replace with your own secure values)
$env:Jwt__Key = "DevSecretKey_ChangeMe_ToA32+CharsRandomValue"
$env:Jwt__Issuer = "SmallHR"
$env:Jwt__Audience = "SmallHRUsers"

# Ensure Development environment
$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host 'Environment configured. Launching API...'
dotnet run --project ..\SmallHR.API


