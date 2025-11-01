#!/usr/bin/env bash
set -euo pipefail

echo "Configuring environment variables for SmallHR (local)..."

# Connection string (adjust as needed)
export ConnectionStrings__DefaultConnection='Server=(localdb)\mssqllocaldb;Database=SmallHRDb;Trusted_Connection=true;MultipleActiveResultSets=true'

# JWT settings (replace with your own secure values)
export Jwt__Key='DevSecretKey_ChangeMe_ToA32+CharsRandomValue'
export Jwt__Issuer='SmallHR'
export Jwt__Audience='SmallHRUsers'

# Ensure Development environment
export ASPNETCORE_ENVIRONMENT='Development'

echo "Environment configured. Launching API..."
dotnet run --project ../SmallHR.API


