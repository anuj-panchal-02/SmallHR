# Database Changes Summary

## Overview
This document summarizes the database changes needed to support the SuperAdmin UI features implemented in the Tenant Administration Console.

## Required Database Changes

### 1. ✅ WebhookEvent Entity (NEW)

**Purpose**: Track billing provider webhook events (Stripe, Paddle, etc.) for the Billing Center page.

**Entity**: `SmallHR.Core/Entities/WebhookEvent.cs`

**Properties**:
- `EventType` (string, max 100) - e.g., "subscription.created", "payment.failed"
- `Provider` (string, max 50) - Stripe, Paddle, PayPal
- `Payload` (string) - JSON payload
- `Processed` (bool) - Whether the webhook has been processed
- `Error` (string, max 2000) - Error message if processing failed
- `Signature` (string, max 500) - Webhook signature for verification
- `TenantId` (int?, nullable) - Associated tenant if applicable
- `SubscriptionId` (int?, nullable) - Associated subscription if applicable

**Indexes**:
- EventType
- Provider
- Processed
- TenantId
- SubscriptionId
- CreatedAt
- Composite: Provider, Processed, CreatedAt

**Migration Required**: ✅ Created - `AddWebhookEventsAndAlerts`

---

### 2. ✅ Alert Entity (NEW)

**Purpose**: Track system alerts (payment failures, overages, errors, suspensions) for the Alerts Hub page.

**Entity**: `SmallHR.Core/Entities/Alert.cs`

**Properties**:
- `TenantId` (int) - Required foreign key
- `AlertType` (string, max 50) - PaymentFailure, Overage, Error, Suspension
- `Severity` (string, max 20) - High, Medium, Low
- `Message` (string, max 500) - Alert message
- `Status` (string, max 20) - Active, Resolved, Acknowledged
- `ResolvedAt` (DateTime?, nullable)
- `ResolvedBy` (string, max 450) - User ID who resolved the alert
- `ResolutionNotes` (string, max 2000)
- `SubscriptionId` (int?, nullable) - Associated subscription if applicable
- `MetadataJson` (string, max 4000) - JSON metadata (amount, limit, usage, errorCode)

**Indexes**:
- TenantId
- AlertType
- Severity
- Status
- CreatedAt
- Composite: TenantId, Status, CreatedAt
- Composite: AlertType, Status
- Composite: Severity, Status

**Migration Required**: ✅ Created - `AddWebhookEventsAndAlerts`

---

## Migration Status

### ✅ Migration Created
- **Migration Name**: `AddWebhookEventsAndAlerts`
- **Status**: Ready to apply
- **Tables Created**: 
  - `WebhookEvents`
  - `Alerts`

### Next Steps

1. **Stop the running application** (if it's running) to release file locks
2. **Apply the migration**:
   ```powershell
   cd SmallHR.API
   dotnet ef database update --project ../SmallHR.Infrastructure --startup-project .
   ```

3. **Verify the migration**:
   ```sql
   -- Check if tables exist
   SELECT name FROM sys.tables WHERE name IN ('WebhookEvents', 'Alerts');
   
   -- Check table structure
   SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
   FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_NAME IN ('WebhookEvents', 'Alerts')
   ORDER BY TABLE_NAME, ORDINAL_POSITION;
   ```

---

## Existing Entities Used (No Changes Needed)

These entities are already in the database and are used by the SuperAdmin UI:

1. ✅ **Tenant** - Already exists
2. ✅ **Subscription** - Already exists
3. ✅ **SubscriptionPlan** - Already exists
4. ✅ **TenantUsageMetrics** - Already exists
5. ✅ **TenantLifecycleEvent** - Already exists
6. ✅ **AdminAudit** - Already exists
7. ✅ **User** - Already exists (Identity)
8. ✅ **Employee** - Already exists

---

## Code Fixes Applied

### Fixed Compilation Errors:
1. ✅ Fixed `await` on `HttpContext.User.FindFirst()` - changed to synchronous call
2. ✅ Fixed `Tenant.Subscriptions` navigation property - changed to query `Subscriptions` table directly
3. ✅ Fixed `TenantId` type mismatch (string vs int) - added `.ToString()` conversion
4. ✅ Fixed `TenantLifecycleEvent.Description` - changed to `Reason` property

---

## Verification Checklist

- [ ] Migration `AddWebhookEventsAndAlerts` has been created
- [ ] Application stopped (if running)
- [ ] Migration applied successfully
- [ ] Tables `WebhookEvents` and `Alerts` exist in database
- [ ] All indexes created correctly
- [ ] Foreign key relationships established
- [ ] Build succeeds without compilation errors

---

## Notes

1. **WebhookEvent** table stores all billing provider webhooks for auditing and reconciliation
2. **Alert** table stores system alerts that can be acknowledged and resolved by SuperAdmin
3. Both tables support JSON metadata for flexible storage of additional information
4. Both tables have proper indexes for efficient querying
5. Foreign keys use `SET NULL` or `CASCADE` delete behavior appropriately

---

## Related Files

- **Entities**: 
  - `SmallHR.Core/Entities/WebhookEvent.cs`
  - `SmallHR.Core/Entities/Alert.cs`
- **DbContext**: 
  - `SmallHR.Infrastructure/Data/ApplicationDbContext.cs` (updated with DbSets and configurations)
- **Migration**: 
  - `SmallHR.Infrastructure/Migrations/AddWebhookEventsAndAlerts.cs` (to be created)

