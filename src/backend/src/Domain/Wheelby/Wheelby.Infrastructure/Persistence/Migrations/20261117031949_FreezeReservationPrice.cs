using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wheelby.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FreezeReservationPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Days",
                schema: "wheelby",
                table: "Reservations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerDayAtBooking",
                schema: "wheelby",
                table: "Reservations",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                schema: "wheelby",
                table: "Reservations",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            // Reservas anteriores a esta migración: se congelan con el precio
            // actual del vehículo (no existe un registro del precio original).
            migrationBuilder.Sql("""
                UPDATE wheelby."Reservations" AS r
                SET "PricePerDayAtBooking" = v."PricePerDay",
                    "Days" = GREATEST(1, CEIL(EXTRACT(EPOCH FROM (r."EndDate" - r."StartDate")) / 86400.0)::integer),
                    "TotalAmount" = ROUND(v."PricePerDay"
                        * GREATEST(1, CEIL(EXTRACT(EPOCH FROM (r."EndDate" - r."StartDate")) / 86400.0)), 2)
                FROM wheelby."Vehicles" AS v
                WHERE v."Id" = r."VehicleId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RenterId",
                schema: "wheelby",
                table: "Reservations",
                column: "RenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_VehicleId",
                schema: "wheelby",
                table: "Reservations",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_RenterId",
                schema: "wheelby",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_VehicleId",
                schema: "wheelby",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Days",
                schema: "wheelby",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PricePerDayAtBooking",
                schema: "wheelby",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                schema: "wheelby",
                table: "Reservations");
        }
    }
}
