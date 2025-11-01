using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallHR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActionFlagsToRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanView",
                table: "RolePermissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanCreate",
                table: "RolePermissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanEdit",
                table: "RolePermissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanDelete",
                table: "RolePermissions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanView",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "CanCreate",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "CanEdit",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "CanDelete",
                table: "RolePermissions");
        }
    }
}


