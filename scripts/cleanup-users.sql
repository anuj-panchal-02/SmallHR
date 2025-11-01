-- =============================================
-- Cleanup Users Script
-- Deletes all users except SuperAdmin
-- =============================================
-- WARNING: This script will delete ALL users except SuperAdmin
-- Make sure you have a backup before running this script
-- =============================================

-- Delete all users from AspNetUserRoles except SuperAdmin
DELETE FROM AspNetUserRoles
WHERE UserId IN (
    SELECT Id FROM AspNetUsers
    WHERE Email != 'superadmin@smallhr.com'
);

-- Delete all users from AspNetUsers except SuperAdmin
DELETE FROM AspNetUsers
WHERE Email != 'superadmin@smallhr.com';

-- Verify: Check remaining users
SELECT 
    Id,
    UserName,
    Email,
    FirstName,
    LastName,
    IsActive,
    CreatedAt
FROM AspNetUsers
ORDER BY CreatedAt;

-- Verify: Check user roles
SELECT 
    u.Email,
    u.UserName,
    r.Name AS Role
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
ORDER BY u.Email;

PRINT 'Cleanup completed. Only SuperAdmin user remains.';

