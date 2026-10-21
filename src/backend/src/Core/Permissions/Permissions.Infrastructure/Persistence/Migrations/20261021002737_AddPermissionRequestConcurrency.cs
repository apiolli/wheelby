using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Permissions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionRequestConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La columna xmin es una columna de sistema de PostgreSQL: ya existe en
            // cada fila y el proveedor de Npgsql no genera DDL para ella.
            // Esta migración solo deja constancia del token de concurrencia en el modelo.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
