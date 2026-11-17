using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wheelby.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NoBlockingOverlap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cierra la carrera entre dos confirmaciones simultáneas: la base rechaza
            // una segunda reserva Confirmed/InProgress que solape fechas del mismo
            // vehículo. Rango [inicio, fin): dos reservas consecutivas no chocan.
            // Status se guarda como texto. WheelbyUnitOfWork traduce la violación a 409.
            // Si ya existen solapes en los datos, esta migración falla: hay que
            // resolverlos (cancelar una de las reservas) antes de aplicarla.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.Sql("""
                ALTER TABLE wheelby."Reservations"
                  ADD CONSTRAINT no_blocking_overlap
                  EXCLUDE USING gist (
                    "VehicleId" WITH =,
                    tstzrange("StartDate", "EndDate", '[)') WITH &&
                  ) WHERE ("Status" IN ('Confirmed', 'InProgress'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE wheelby.\"Reservations\" DROP CONSTRAINT IF EXISTS no_blocking_overlap;");
        }
    }
}
