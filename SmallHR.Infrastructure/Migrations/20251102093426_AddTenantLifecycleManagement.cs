using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallHR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantLifecycleManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GracePeriodEndsAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaddleCustomerId",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDeletionAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspendedAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantLifecycleEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriggeredBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantLifecycleEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantLifecycleEvents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantLifecycleEvents_EventDate",
                table: "TenantLifecycleEvents",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_TenantLifecycleEvents_EventType",
                table: "TenantLifecycleEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_TenantLifecycleEvents_TenantId",
                table: "TenantLifecycleEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantLifecycleEvents_TenantId_EventDate",
                table: "TenantLifecycleEvents",
                columns: new[] { "TenantId", "EventDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantLifecycleEvents");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "GracePeriodEndsAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PaddleCustomerId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ScheduledDeletionAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SuspendedAt",
                table: "Tenants");
        }
    }
}
