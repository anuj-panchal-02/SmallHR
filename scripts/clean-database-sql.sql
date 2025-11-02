-- =============================================
-- Clean Database SQL Script
-- Removes all data except roles and creates 1 SuperAdmin
-- =============================================
-- WARNING: This script will delete ALL data except roles
-- Make sure you have a backup before running this script
-- =============================================

USE SmallHRDb;
GO

BEGIN TRANSACTION;

-- 1. Delete all AdminAudit logs
DELETE FROM AdminAudits;
PRINT 'Deleted all AdminAudit logs';

-- 2. Delete all TenantLifecycleEvents
DELETE FROM TenantLifecycleEvents;
PRINT 'Deleted all TenantLifecycleEvents';

-- 3. Delete all TenantUsageMetrics
DELETE FROM TenantUsageMetrics;
PRINT 'Deleted all TenantUsageMetrics';

-- 4. Delete all SubscriptionPlanFeatures
DELETE FROM SubscriptionPlanFeatures;
PRINT 'Deleted all SubscriptionPlanFeatures';

-- 5. Delete all Subscriptions
DELETE FROM Subscriptions;
PRINT 'Deleted all Subscriptions';

-- 6. Delete all SubscriptionPlans (if needed - usually keep plans)
-- DELETE FROM SubscriptionPlans;
-- PRINT 'Deleted all SubscriptionPlans';

-- 7. Delete all Features (if needed - usually keep features)
-- DELETE FROM Features;
-- PRINT 'Deleted all Features';

-- 8. Delete all RolePermissions
DELETE FROM RolePermissions;
PRINT 'Deleted all RolePermissions';

-- 9. Delete all Modules
DELETE FROM Modules;
PRINT 'Deleted all Modules';

-- 10. Delete all Positions
DELETE FROM Positions;
PRINT 'Deleted all Positions';

-- 11. Delete all Departments
DELETE FROM Departments;
PRINT 'Deleted all Departments';

-- 12. Delete all Attendances
DELETE FROM Attendances;
PRINT 'Deleted all Attendances';

-- 13. Delete all LeaveRequests
DELETE FROM LeaveRequests;
PRINT 'Deleted all LeaveRequests';

-- 14. Delete all Employees
DELETE FROM Employees;
PRINT 'Deleted all Employees';

-- 15. Delete all Tenants (except system tenant if exists)
DELETE FROM Tenants;
PRINT 'Deleted all Tenants';

-- 16. Delete all users from AspNetUserRoles except SuperAdmin
DELETE FROM AspNetUserRoles
WHERE UserId IN (
    SELECT Id FROM AspNetUsers
    WHERE Email != 'superadmin@smallhr.com'
);
PRINT 'Deleted all user roles except SuperAdmin';

-- 17. Delete all users from AspNetUsers except SuperAdmin
DELETE FROM AspNetUsers
WHERE Email != 'superadmin@smallhr.com';
PRINT 'Deleted all users except SuperAdmin';

-- 18. Ensure SuperAdmin exists and has correct TenantId
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'superadmin@smallhr.com')
BEGIN
    DECLARE @SuperAdminId NVARCHAR(450);
    DECLARE @SuperAdminRoleId NVARCHAR(450);
    
    -- Create SuperAdmin user
    SET @SuperAdminId = NEWID();
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                             PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, 
                             TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FirstName, LastName, 
                             DateOfBirth, IsActive, TenantId)
    VALUES (
        @SuperAdminId,
        'superadmin@smallhr.com',
        'SUPERADMIN@SMALLHR.COM',
        'superadmin@smallhr.com',
        'SUPERADMIN@SMALLHR.COM',
        1, -- EmailConfirmed
        'AQAAAAIAAYagAAAAELvRgXBhXm+W2h3Uj4J9v5L5nZ8QpF3hY5ZxW9vK2nM8PqR4T6Y7U8V0W2X4Y6Z8A=', -- SuperAdmin@123
        NEWID(),
        NEWID(),
        0, -- PhoneNumberConfirmed
        0, -- TwoFactorEnabled
        1, -- LockoutEnabled
        0, -- AccessFailedCount
        'Super',
        'Admin',
        '1985-01-01',
        1, -- IsActive
        NULL -- TenantId (SuperAdmin has no tenant)
    );
    
    -- Get SuperAdmin role ID
    SELECT @SuperAdminRoleId = Id FROM AspNetRoles WHERE Name = 'SuperAdmin';
    
    IF @SuperAdminRoleId IS NOT NULL
    BEGIN
        -- Assign SuperAdmin role
        INSERT INTO AspNetUserRoles (UserId, RoleId)
        VALUES (@SuperAdminId, @SuperAdminRoleId);
        PRINT 'Created SuperAdmin user and assigned role';
    END
    ELSE
    BEGIN
        PRINT 'Warning: SuperAdmin role not found. Please create roles first.';
    END
END
ELSE
BEGIN
    -- Update existing SuperAdmin to ensure TenantId is NULL
    UPDATE AspNetUsers
    SET TenantId = NULL
    WHERE Email = 'superadmin@smallhr.com' AND TenantId IS NOT NULL;
    PRINT 'Updated existing SuperAdmin to have TenantId = NULL';
END

COMMIT TRANSACTION;

-- Verify: Check remaining users
PRINT '';
PRINT 'Verification - Remaining Users:';
SELECT 
    Id,
    UserName,
    Email,
    FirstName,
    LastName,
    IsActive,
    TenantId,
    CreatedAt
FROM AspNetUsers
ORDER BY CreatedAt;

-- Verify: Check user roles
PRINT '';
PRINT 'Verification - User Roles:';
SELECT 
    u.Email,
    u.UserName,
    r.Name AS Role,
    u.TenantId
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
ORDER BY u.Email;

-- Verify: Check roles
PRINT '';
PRINT 'Verification - All Roles:';
SELECT Id, Name, NormalizedName FROM AspNetRoles ORDER BY Name;

PRINT '';
PRINT '=============================================';
PRINT 'Cleanup completed successfully!';
PRINT 'Only SuperAdmin user (superadmin@smallhr.com) remains.';
PRINT 'Password: SuperAdmin@123';
PRINT '=============================================';

