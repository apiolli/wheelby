using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Permissions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "permissions");

            migrationBuilder.CreateTable(
                name: "PermissionGrants",
                schema: "permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionGrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionRequests",
                schema: "permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_UserId_PermissionCode",
                schema: "permissions",
                table: "PermissionGrants",
                columns: new[] { "UserId", "PermissionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_RequesterId_PermissionCode",
                schema: "permissions",
                table: "PermissionRequests",
                columns: new[] { "RequesterId", "PermissionCode" },
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermissionGrants",
                schema: "permissions");

            migrationBuilder.DropTable(
                name: "PermissionRequests",
                schema: "permissions");
        }
    }
}
