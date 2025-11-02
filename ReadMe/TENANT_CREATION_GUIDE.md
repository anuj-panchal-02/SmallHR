# Tenant Creation Guide (SaaS-Ready)

This guide explains how to create and onboard a new tenant in SmallHR for both Shared Database (TenantId-per-row) and Database-per-Tenant strategies.

- Multi-tenancy strategy used by default: Shared DB with `TenantId` (fastest, lowest cost)
- Upgrade path supported: Database-per-tenant via `IConnectionResolver`

---

## 0) Prerequisites

- API running locally (default): http://localhost:5192
- SuperAdmin account (see `LOGIN_CREDENTIALS.md` or seed users)
- JWT token obtained via `POST /api/auth/login`
- For DB-per-tenant (optional): SQL Server access to create a new database

Useful headers for API calls:
- `Authorization: Bearer <JWT>` (for protected endpoints)
- `X-Tenant-Id: <tenantKey>` (e.g., `acme`) OR `X-Tenant-Domain: <domain>` (e.g., `acme.local`)

---

## 1) Create the Tenant (SuperAdmin only)

Endpoint:
- `POST /api/tenants`

Body:
```json
{ "name": "acme", "domain": "acme.local", "isActive": true }
```
Notes:
- `name` becomes your canonical `tenantId` (we use lowercase in practice)
- `domain` is optional; use it if you’ll resolve by subdomain or host header

Verify:
- `GET /api/tenants` to list
- `GET /api/tenants/{id}` for the new tenant

---

## 2) Choose Isolation Strategy

### Option A: Shared Database (default)
- No extra infra required
- All data stored in the shared DB with `TenantId` stamped automatically
- Proceed to Step 3

### Option B: Database-Per-Tenant (stronger isolation)
**Note**: This is an optional advanced feature. By default, the system uses Shared Database with TenantId isolation.

1) Create DB (e.g., `SmallHRDb_TenantName`)
2) Apply EF migrations to that DB:
```powershell
# One-time design-time connection for tools
setx EF_CONNECTION "Server=...;Database=SmallHRDb_TenantName;User Id=...;Password=...;TrustServerCertificate=True"
$env:EF_CONNECTION="Server=...;Database=SmallHRDb_TenantName;User Id=...;Password=...;TrustServerCertificate=True"

# Apply migrations
cd smallHR
 dotnet ef database update `
  --project .\SmallHR.Infrastructure\ `
  --startup-project .\SmallHR.API\ `
  --context ApplicationDbContext
```
3) Map tenant to DB in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=...;Database=SmallHRDb;...",
  "Tenants": {
    "tenantname": "Server=...;Database=SmallHRDb_TenantName;User Id=...;Password=...;TrustServerCertificate=True"
  }
}
```
4) Restart API

---

## 3) Seed Modules for the Tenant

Modules power the navigation tree. Seed per-tenant:

Request:
- `POST /api/modules/seed`
- Headers: `Authorization: Bearer <JWT>`, `X-Tenant-Id: acme`

Result:
- Default navigation created for the tenant

---

## 4) Login as the Tenant

Always include tenant header on login so the token embeds the tenant claim:

Request:
- `POST /api/auth/login`
- Headers: `X-Tenant-Id: acme`
- Body: `{ "email": "superadmin@smallhr.com", "password": "SuperAdmin@123" }`

Response:
- JWT now contains `"tenant": "acme"`
- All subsequent calls must include the same `X-Tenant-Id: acme`; mismatch returns 403

---

## 5) Optional: Create Tenant Admin User

- Register a user (or convert an existing user) as the tenant’s admin
- Assign role `Admin` to that user
- This user can manage tenant-specific resources (employees, attendance, etc.)

---

## 6) Frontend Integration

- For subdomain routing (e.g., `acme.your-saas.com`) resolve the tenant on the frontend, then send the proper header on every API call:
  - `X-Tenant-Id: acme` or `X-Tenant-Domain: acme.your-saas.com`
- The backend enforces tenant boundary (token tenant must match header tenant)

---

## 7) Verification Checklist

- `GET /api/modules` with `X-Tenant-Id: acme` returns seeded modules
- Creating records (employees, leave) automatically stamps `TenantId`
- Switching tenants via headers shows isolated data sets
- `403 Tenant mismatch` triggered when header and token differ (expected)

---

## 8) Operational Runbooks (SaaS)

- Add a new tenant
  1. `POST /api/tenants`
  2. (Optional) Create DB and map `ConnectionStrings:Tenants:{tenantId}`
  3. `POST /api/modules/seed` with tenant header
  4. Create admin user and send invite

- Move tenant to dedicated DB (later)
  1. Create and migrate DB
  2. Copy tenant’s data
  3. Add mapping in `ConnectionStrings:Tenants`
  4. Restart API and smoke test

- Suspend or reactivate tenant
  - `PUT /api/tenants/{id}` with `IsActive` flag

---

## 9) Monitoring & Security

- Logs & metrics should include `tenantId` for correlation
- Monitor:
  - 403 boundary rejections (tenant mismatch)
  - 5xx per tenant
  - DB errors per tenant
- Only SuperAdmin can manage tenants; tenants see only their own data

---

## 10) Troubleshooting

- 403 Tenant mismatch
  - Ensure both header and JWT use the same tenant
  - Re-login with correct tenant header

- Navigation empty
  - Call `POST /api/modules/seed` for that tenant

- DB-per-tenant not used
  - Check `ConnectionStrings:Tenants:{tenantId}` exists and API restarted

---

## 11) API Quick Reference

- Create tenant: `POST /api/tenants`
- Update tenant: `PUT /api/tenants/{id}`
- Map domain: `PUT /api/tenants/{id}/domain`
- Seed modules: `POST /api/modules/seed`
- Login: `POST /api/auth/login`

Headers to include for tenant context:
- `X-Tenant-Id: <tenantKey>` or `X-Tenant-Domain: <domain>`
- `Authorization: Bearer <JWT>`

---

That’s it—tenants can be onboarded in minutes, and you can selectively move high-value tenants to their own database by adding a single connection mapping.

