using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallHR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureSuperAdminTenantIdNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure all SuperAdmin users have TenantId = NULL
            // SuperAdmin operates at platform layer, not tenant layer
            migrationBuilder.Sql(@"
                UPDATE [AspNetUsers]
                SET [TenantId] = NULL
                WHERE [Id] IN (
                    SELECT ur.[UserId]
                    FROM [AspNetUserRoles] ur
                    INNER JOIN [AspNetRoles] r ON ur.[RoleId] = r.[Id]
                    WHERE r.[Name] = 'SuperAdmin'
                )
                AND [TenantId] IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed - this is a data migration to fix SuperAdmin users
            // We don't know what the original TenantId values were, so we can't restore them
        }
    }
}
