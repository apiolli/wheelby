using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QueuedEmailRetries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                schema: "notifications",
                table: "QueuedEmails",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FailedAt",
                schema: "notifications",
                table: "QueuedEmails",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptAt",
                schema: "notifications",
                table: "QueuedEmails",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                schema: "notifications",
                table: "QueuedEmails",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attempts",
                schema: "notifications",
                table: "QueuedEmails");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                schema: "notifications",
                table: "QueuedEmails");

            migrationBuilder.DropColumn(
                name: "LastAttemptAt",
                schema: "notifications",
                table: "QueuedEmails");

            migrationBuilder.DropColumn(
                name: "LastError",
                schema: "notifications",
                table: "QueuedEmails");
        }
    }
}
