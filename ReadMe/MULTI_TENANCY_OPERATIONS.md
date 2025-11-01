# Multi-Tenancy: Operations, Migration, and Monitoring

## Overview
SmallHR now supports multi-tenancy using shared-database with TenantId and is architected to upgrade to database-per-tenant via `IConnectionResolver`.

- Tenant resolution: `X-Tenant-Id` or `X-Tenant-Domain` header; default is `default`.
- Data isolation: TenantId on aggregates; EF global query filters; automatic TenantId stamping in SaveChanges.
- Auth boundary: JWT contains `tenant` claim; middleware enforces header vs claim match.
- Optional db-per-tenant: configure per-tenant connection strings under `ConnectionStrings:Tenants:{tenantId}`.

---

## Migration/Rollout Plan

1) Prepare
- Back up database(s)
- Tag current release
- Communicate maintenance window (if needed)

2) Deploy code changes
- Deploy API with new multi-tenancy code
- Ensure connection strings are set: `DefaultConnection`, and optional `ConnectionStrings:Tenants:{tenantId}` for early adopters

3) Apply EF migrations
- Run: `dotnet ef database update --project .\SmallHR.Infrastructure\ --startup-project .\SmallHR.API\`
- Confirms `Tenants` table and `TenantId` columns exist with indexes

4) Seed tenants and modules (per tenant)
- As SuperAdmin: create tenants via `POST /api/tenants`
- Map domains via `PUT /api/tenants/{id}/domain`
- Seed modules per tenant: `POST /api/modules/seed` with appropriate `X-Tenant-Id` header and JWT

5) Data backfill (existing data)
- Set `TenantId='default'` for legacy rows
- Optionally split data to multiple tenants if you’re migrating customers to dedicated tenants

6) Cutover (if moving some to db-per-tenant)
- For each tenant to isolate:
  - Create database and migrate schema
  - Add `ConnectionStrings:Tenants:{tenantId}`
  - Move data for that tenant from shared DB to dedicated DB (tools: BCP/SSIS/Scripts)
  - Smoke test tenant routes using headers

7) Post-deploy validation
- Verify requests with and without headers are properly scoped
- Verify role permissions and modules load per tenant

---

## Local Setup

1) Start services
- API: `dotnet run --project .\SmallHR.API\SmallHR.API.csproj`
- Web: `cd SmallHR.Web && npm run dev`

2) Login and seed
- Login: `POST /api/auth/login` (use provided demo creds)
- For seeding per tenant:
  - Set header `X-Tenant-Id: default`
  - `POST /api/modules/seed` with JWT

3) Create new tenant
- `POST /api/tenants` (SuperAdmin only)
- Example:
```json
{ "name": "acme", "domain": "acme.local", "isActive": true }
```

4) Test tenant isolation
- Include `X-Tenant-Id: acme` and login; subsequent calls must include the same header
- Ensure tenant mismatch returns 403

---

## Configuration

- appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=SmallHR;...",
    "Tenants": {
      "acme": "Server=...;Database=SmallHR_Acme;..."
    }
  }
}
```
- CORS includes frontend origins (Vite ports)

---

## Monitoring & Alerting

- Middleware boundary rejections
  - Count 403 responses with message "Tenant mismatch" as signal of misrouted clients
- Database per tenant
  - Track connection usage per tenant (via logs) to confirm correct routing
- Cache warmup job
  - `ModulesWarmupHostedService` logs failures; hook to your APM
- Key Metrics
  - 5xx rate per tenant
  - Auth failures and 403 boundary rejections per tenant
  - DB connection errors per tenant
- Logs
  - Enrich logs with `tenantId` (from `ITenantProvider`) to correlate easily

---

## Operational Runbooks

- Add a new tenant
  1. `POST /api/tenants` (SuperAdmin)
  2. Optional: add dedicated connection under `ConnectionStrings:Tenants:{tenantId}`
  3. `POST /api/modules/seed` for that tenant

- Rotate a tenant to dedicated DB
  1. Create DB and apply migrations
  2. Migrate data for that tenant
  3. Add connection string mapping
  4. Restart API or refresh configuration if using dynamic reload

- Rollback plan
  - Repoint tenant in `ConnectionStrings:Tenants` to shared DB
  - Restore data from backup if necessary

---

## Security Notes

- Only SuperAdmin can manage tenants
- JWT contains `tenant` claim; boundary enforced by middleware
- EF filters prevent cross-tenant reads; SaveChanges stamping prevents wrong-tenant writes

---

## Future Enhancements

- Dynamic configuration source for tenant connections (KeyVault/Config Server)
- `ITenantProvider` from subdomain parsing (e.g., acme.example.com)
- Observability: tenantId as a structured log field for all requests
- Blue/Green per tenant deployments (if db-per-tenant)
