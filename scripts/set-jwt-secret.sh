#!/bin/bash
# Set JWT Secret for Development
# This script sets the JWT_SECRET_KEY environment variable

echo "Setting JWT_SECRET_KEY environment variable..."

# Generate a secure random key (32+ characters)
JWT_SECRET="SmallHR_SuperSecure_JWT_SecretKey_2025_AtLeast32CharactersLong_Required"

# Set for current session
export JWT_SECRET_KEY="$JWT_SECRET"

echo "✅ JWT_SECRET_KEY set for current session"
echo "   Value: $JWT_SECRET"
echo ""
echo "⚠️  Note: This is only set for the current session."
echo "   To make it persistent, choose one of the options below:"
echo ""
echo "Option 1: Use dotnet user-secrets (Recommended for development):"
echo "   dotnet user-secrets set \"Jwt:Key\" \"$JWT_SECRET\""
echo ""
echo "Option 2: Add to ~/.bashrc or ~/.zshrc:"
echo "   export JWT_SECRET_KEY=\"$JWT_SECRET\""
echo ""
echo "Option 3: Add to appsettings.Development.json (Less secure, not recommended):"
echo "   Add: \"Jwt: { \"Key\": \"$JWT_SECRET\" }\""

