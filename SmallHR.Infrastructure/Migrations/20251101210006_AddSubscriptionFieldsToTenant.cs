using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallHR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionFieldsToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSubscriptionActive",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxEmployees",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionEndDate",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionPlan",
                table: "Tenants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Free");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionStartDate",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            // Set defaults for existing tenants
            migrationBuilder.Sql(@"
                UPDATE Tenants 
                SET SubscriptionPlan = 'Free',
                    MaxEmployees = 10,
                    IsSubscriptionActive = 1,
                    SubscriptionStartDate = GETUTCDATE(),
                    SubscriptionEndDate = DATEADD(YEAR, 1, GETUTCDATE())
                WHERE SubscriptionPlan IS NULL OR SubscriptionPlan = ''
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSubscriptionActive",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MaxEmployees",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionEndDate",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlan",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionStartDate",
                table: "Tenants");
        }
    }
}
