using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDisabledAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DisabledAt",
                schema: "access_control",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisabledAt",
                schema: "access_control",
                table: "Users");
        }
    }
}
