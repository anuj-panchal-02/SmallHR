# Start SmallHR Development Environment
# This script starts both API and Frontend in separate windows

Write-Host "🚀 Starting SmallHR Development Environment..." -ForegroundColor Green

# Get the project root directory
$projectRoot = Split-Path -Parent $PSScriptRoot

# Start API in first window
Write-Host "Starting API server..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$projectRoot\SmallHR.API'; dotnet run" -WindowStyle Normal

# Wait a moment for API to start
Start-Sleep -Seconds 3

# Start Frontend in second window
Write-Host "Starting Frontend server..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$projectRoot\SmallHR.Web'; npm run dev" -WindowStyle Normal

Write-Host "✅ Both servers are starting..." -ForegroundColor Green
Write-Host ""
Write-Host "API will be available at: https://localhost:7082" -ForegroundColor Cyan
Write-Host "Frontend will be available at: http://localhost:5173" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to stop both servers..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Stop all running instances
Write-Host ""
Write-Host "Stopping servers..." -ForegroundColor Yellow
Get-Process | Where-Object {$_.Path -like "*SmallHR*" -or $_.CommandLine -like "*SmallHR*"} | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "✅ Servers stopped" -ForegroundColor Green


