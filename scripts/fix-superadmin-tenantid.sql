-- Fix SuperAdmin Users: Set TenantId = NULL
-- SuperAdmin users operate at the platform layer, not tenant layer
-- They should NOT have a TenantId

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

