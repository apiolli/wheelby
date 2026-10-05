using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiresAt",
                schema: "access_control",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                schema: "access_control",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenIssuedAt",
                schema: "access_control",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenUsedAt",
                schema: "access_control",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresAt",
                schema: "access_control",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                schema: "access_control",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenIssuedAt",
                schema: "access_control",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenUsedAt",
                schema: "access_control",
                table: "Users");
        }
    }
}
