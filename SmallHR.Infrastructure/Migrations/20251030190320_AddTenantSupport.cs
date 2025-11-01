using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallHR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleName_PagePath",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_Modules_ParentPath_DisplayOrder",
                table: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_Modules_Path",
                table: "Modules");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "RolePermissions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Modules",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_TenantId_RoleName_PagePath",
                table: "RolePermissions",
                columns: new[] { "TenantId", "RoleName", "PagePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_TenantId_ParentPath_DisplayOrder",
                table: "Modules",
                columns: new[] { "TenantId", "ParentPath", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Modules_TenantId_Path",
                table: "Modules",
                columns: new[] { "TenantId", "Path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Domain",
                table: "Tenants",
                column: "Domain");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_TenantId_RoleName_PagePath",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_Modules_TenantId_ParentPath_DisplayOrder",
                table: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_Modules_TenantId_Path",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Modules");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleName_PagePath",
                table: "RolePermissions",
                columns: new[] { "RoleName", "PagePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_ParentPath_DisplayOrder",
                table: "Modules",
                columns: new[] { "ParentPath", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Modules_Path",
                table: "Modules",
                column: "Path",
                unique: true);
        }
    }
}
