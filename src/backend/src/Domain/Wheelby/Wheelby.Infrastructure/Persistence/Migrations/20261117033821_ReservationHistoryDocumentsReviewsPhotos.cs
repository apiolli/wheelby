using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wheelby.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReservationHistoryDocumentsReviewsPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "wheelby",
                table: "Vehicles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "wheelby",
                table: "Vehicles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "wheelby",
                table: "Vehicles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "wheelby",
                table: "Vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Car");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "wheelby",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReservationDocuments",
                schema: "wheelby",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttachedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    AttachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationDocuments_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalSchema: "wheelby",
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReservationStatusHistory",
                schema: "wheelby",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    From = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    To = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Actor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationStatusHistory_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalSchema: "wheelby",
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                schema: "wheelby",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.CheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_Reviews_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalSchema: "wheelby",
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehiclePhotos",
                schema: "wheelby",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehiclePhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehiclePhotos_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "wheelby",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationDocuments_ReservationId_DocumentId",
                schema: "wheelby",
                table: "ReservationDocuments",
                columns: new[] { "ReservationId", "DocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationStatusHistory_ReservationId_At",
                schema: "wheelby",
                table: "ReservationStatusHistory",
                columns: new[] { "ReservationId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReservationId_AuthorId",
                schema: "wheelby",
                table: "Reviews",
                columns: new[] { "ReservationId", "AuthorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_VehicleId_CreatedAt",
                schema: "wheelby",
                table: "Reviews",
                columns: new[] { "VehicleId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePhotos_VehicleId_DocumentId",
                schema: "wheelby",
                table: "VehiclePhotos",
                columns: new[] { "VehicleId", "DocumentId" },
                unique: true);

            // Reservas anteriores al historial: la fila de creación y, si ya no
            // están Pending, una fila con su estado actual. Se desconoce quién hizo
            // ese cambio y cuándo, así que queda como System en la fecha de creación.
            migrationBuilder.Sql("""
                INSERT INTO wheelby."ReservationStatusHistory" ("Id", "ReservationId", "From", "To", "Actor", "ActorId", "Reason", "At")
                SELECT gen_random_uuid(), r."Id", NULL, 'Pending', 'Renter', r."RenterId", NULL, r."CreatedAt"
                FROM wheelby."Reservations" AS r;

                INSERT INTO wheelby."ReservationStatusHistory" ("Id", "ReservationId", "From", "To", "Actor", "ActorId", "Reason", "At")
                SELECT gen_random_uuid(), r."Id", 'Pending', r."Status", 'System', NULL,
                       'Estado previo a la creación del historial.', r."CreatedAt"
                FROM wheelby."Reservations" AS r
                WHERE r."Status" <> 'Pending';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservationDocuments",
                schema: "wheelby");

            migrationBuilder.DropTable(
                name: "ReservationStatusHistory",
                schema: "wheelby");

            migrationBuilder.DropTable(
                name: "Reviews",
                schema: "wheelby");

            migrationBuilder.DropTable(
                name: "VehiclePhotos",
                schema: "wheelby");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "wheelby",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "wheelby",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "wheelby",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "wheelby",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "wheelby",
                table: "Vehicles");
        }
    }
}
