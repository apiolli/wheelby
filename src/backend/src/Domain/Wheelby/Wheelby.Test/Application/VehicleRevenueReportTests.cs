using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Wheelby.Application.Features.VehicleRevenueReport;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Vehicles;
using Wheelby.Infrastructure;
using Wheelby.Infrastructure.Persistence;
using Wheelby.Test.TestSupport;

// RF-REP-02 a 05: ¿cuánto generó cada vehículo y cuántos días estuvo alquilado en un período?
public class VehicleRevenueReportTests
{
    private static readonly DateTime Publicado = new(2029, 12, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Ana = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Carla = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Beto = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");
    private static readonly Guid Admin = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000a");

    private static DateTime Dia(int year, int month, int day) => new(year, month, day, 10, 0, 0, DateTimeKind.Utc);

    // Reserva de Beto en el estado pedido, recorriendo la máquina de estados con fechas válidas.
    private static Reservation Reserva(Vehicle v, DateTime inicio, int dias, ReservationStatus estado, decimal precio = 1000m)
    {
        var r = Reservation.Create(v.Id, Beto, inicio, inicio.AddDays(dias), precio, Publicado);
        if (estado == ReservationStatus.Pending)
            return r;

        if (estado == ReservationStatus.Cancelled)
        {
            r.TransitionTo(ReservationStatus.Cancelled, ReservationActor.Renter, Publicado);
            return r;
        }

        r.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, Publicado);
        if (estado == ReservationStatus.Confirmed)
            return r;

        r.TransitionTo(ReservationStatus.InProgress, ReservationActor.Host, inicio);
        if (estado == ReservationStatus.InProgress)
            return r;

        r.TransitionTo(ReservationStatus.Completed, ReservationActor.Host, inicio.AddDays(dias));
        return r;
    }

    private static async Task<WheelbyDbContext> BaseAsync(Vehicle[] vehiculos, params Reservation[] reservas)
    {
        var db = WheelbyDb.CreaContexto();
        db.Vehicles.AddRange(vehiculos);
        db.Reservations.AddRange(reservas);
        await db.SaveChangesAsync();
        return db;
    }

    private static VehicleRevenueReportQueryHandler Handler(WheelbyDbContext db, Guid userId, string role)
        => new(new VehicleRevenueReportStore(db), new MutableCurrentUser(userId, role));

    [Fact]
    public async Task RF_REP_02_suma_ingresos_y_dias_por_vehiculo()
    {
        // Arrange
        var corolla = Vehicle.Create(Ana, "Toyota", "Corolla", 2020, "A-100", 1000m, Publicado);
        var civic = Vehicle.Create(Ana, "Honda", "Civic", 2021, "A-200", 2000m, Publicado);
        var db = await BaseAsync([corolla, civic],
            Reserva(corolla, Dia(2030, 1, 5), 2, ReservationStatus.Confirmed, 1000m),
            Reserva(corolla, Dia(2030, 1, 15), 3, ReservationStatus.Completed, 1000m),
            Reserva(civic, Dia(2030, 1, 20), 4, ReservationStatus.InProgress, 2000m));

        // Act
        var report = await Handler(db, Admin, AccessControlRoles.Administrator)
            .Handle(new VehicleRevenueReportQuery("2030-01-01", "2030-02-01"), CancellationToken.None);

        // Assert
        Assert.Equal("all", report.Scope);
        Assert.Equal("DOP", report.Currency);
        Assert.Equal(2, report.Rows.Count);
        var primero = report.Rows[0];
        Assert.Equal(civic.Id, primero.VehicleId);
        Assert.Equal(("Honda", "Civic", "A-200"), (primero.Brand, primero.Model, primero.Plate));
        Assert.Equal(1, primero.Reservations);
        Assert.Equal(4, primero.Days);
        Assert.Equal(8000m, primero.TotalAmount);
        Assert.Equal(8000m, primero.AverageAmount);
        var segundo = report.Rows[1];
        Assert.Equal(corolla.Id, segundo.VehicleId);
        Assert.Equal(2, segundo.Reservations);
        Assert.Equal(5, segundo.Days);
        Assert.Equal(5000m, segundo.TotalAmount);
        Assert.Equal(2500m, segundo.AverageAmount);
        Assert.Equal(new VehicleRevenueTotals(3, 9, 13000m), report.Totals);
    }

    [Fact]
    public async Task RF_REP_02_excluye_pendientes_y_canceladas()
    {
        // Arrange
        var corolla = Vehicle.Create(Ana, "Toyota", "Corolla", 2020, "A-100", 1000m, Publicado);
        var db = await BaseAsync([corolla],
            Reserva(corolla, Dia(2030, 1, 5), 2, ReservationStatus.Pending),
            Reserva(corolla, Dia(2030, 1, 8), 2, ReservationStatus.Cancelled),
            Reserva(corolla, Dia(2030, 1, 12), 2, ReservationStatus.Confirmed));

        // Act
        var report = await Handler(db, Admin, AccessControlRoles.Administrator)
            .Handle(new VehicleRevenueReportQuery("2030-01-01", "2030-02-01"), CancellationToken.None);

        // Assert
        var fila = Assert.Single(report.Rows);
        Assert.Equal(1, fila.Reservations);
        Assert.Equal(2000m, fila.TotalAmount);
    }

    [Fact]
    public async Task RF_REP_02_usa_el_monto_congelado_y_no_el_precio_actual()
    {
        // Arrange
        var corolla = Vehicle.Create(Ana, "Toyota", "Corolla", 2020, "A-100", 1000m, Publicado);
        var reserva = Reserva(corolla, Dia(2030, 1, 5), 2, ReservationStatus.Confirmed, 1000m);
        corolla.Update("Toyota", "Corolla", 2020, "A-100", 9999m, corolla.Type, corolla.City, corolla.Description, Publicado);
        var db = await BaseAsync([corolla], reserva);

        // Act
        var report = await Handler(db, Admin, AccessControlRoles.Administrator)
            .Handle(new VehicleRevenueReportQuery("2030-01-01", "2030-02-01"), CancellationToken.None);

        // Assert
        Assert.Equal(2000m, Assert.Single(report.Rows).TotalAmount);
    }

    [Fact]
    public async Task RF_REP_03_varios_registros_producen_una_sola_fila()
    {
        // Arrange
        var corolla = Vehicle.Create(Ana, "Toyota", "Corolla", 2020, "A-100", 1000m, Publicado);
        var db = await BaseAsync([corolla],
            Reserva(corolla, Dia(2030, 1, 3), 1, ReservationStatus.Confirmed),
            Reserva(corolla, Dia(2030, 1, 10), 1, ReservationStatus.Confirmed),
            Reserva(corolla, Dia(2030, 1, 20), 1, ReservationStatus.Completed));

        // Act
        var report = await Handler(db, Admin, AccessControlRoles.Administrator)
            .Handle(new VehicleRevenueReportQuery("2030-01-01", "2030-02-01"), CancellationToken.None);

        // Assert
        var fila = Assert.Single(report.Rows);
        Assert.Equal(3, fila.Reservations);
        Assert.Equal(3, fila.Days);
    }

    [Fact]
    public async Task RF_REP_04_anfitrion_solo_ve_sus_vehiculos()
    {
        // Arrange
        var deAna = Vehicle.Create(Ana, "Toyota", "Corolla", 2020, "A-100", 1000m, Publicado);
        var deCarla = Vehicle.Create(Carla, "Kia", "Rio", 2019, "C-100", 1500m, Publicado);
        var db = await BaseAsync([deAna, deCarla],
            Reserva(deAna, Dia(2030, 1, 5), 2, ReservationStatus.Confirmed),
            Reserva(deCarla, Dia(2030, 1, 6), 2, ReservationStatus.Confirmed));
        var query = new VehicleRevenueReportQuery("2030-01-01", "2030-02-01");

        // Act
        var deAnaReport = await Handler(db, Ana, AccessControlRoles.Standard).Handle(query, CancellationToken.None);
        var deBetoReport = await Handler(db, Beto, AccessControlRoles.Standard).Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal("own", deAnaReport.Scope);
        Assert.Equal(deAna.Id, Assert.Single(deAnaReport.Rows).VehicleId);
        Assert.Equal("own", deBetoReport.Scope);
        Assert.Empty(deBetoReport.Rows);
        Assert.Equal(new VehicleRevenueTotals(0, 0, 0m), deBetoReport.Totals);
    }

    [Fact]
    public async Task RF_REP_04_administrador_ve_todos_los_vehiculos()
    {
        // Arrange
        var deAna = Vehicle.Create(Ana, "Toyota", "Corolla", 2020, "A-100", 1000m, Publicado);
        var deCarla = Vehicle.Create(Carla, "Kia", "Rio", 2019, "C-100", 1500m, Publicado);
        var db = await BaseAsync([deAna, deCarla],
            Reserva(deAna, Dia(2030, 1, 5), 2, ReservationStatus.Confirmed, 1000m),
            Reserva(deCarla, Dia(2030, 1, 6), 2, ReservationStatus.Confirmed, 1500m));

        // Act
        var report = await Handler(db, Admin, AccessControlRoles.Administrator)
            .Handle(new VehicleRevenueReportQuery("2030-01-01", "2030-02-01"), CancellationToken.None);

        // Assert
        Assert.Equal("all", report.Scope);
        Assert.Equal(new[] { deCarla.Id, deAna.Id }, report.Rows.Select(r => r.VehicleId));
    }

    [Fact]
    public async Task RF_REP_05_dos_rangos_distintos_dan_resultados_distintos()
    {
        // Arrange
        var corolla = Vehicle.Create(Ana, "Toyota", "Corolla", 2020, "A-100", 1000m, Publicado);
        var db = await BaseAsync([corolla],
            Reserva(corolla, Dia(2030, 1, 10), 2, ReservationStatus.Confirmed),
            Reserva(corolla, Dia(2030, 2, 10), 3, ReservationStatus.Confirmed),
            Reserva(corolla, Dia(2030, 2, 20), 1, ReservationStatus.Confirmed));
        var handler = Handler(db, Admin, AccessControlRoles.Administrator);

        // Act
        var enero = await handler.Handle(new VehicleRevenueReportQuery("2030-01-01", "2030-02-01"), CancellationToken.None);
        var febrero = await handler.Handle(new VehicleRevenueReportQuery("2030-02-01", "2030-03-01"), CancellationToken.None);

        // Assert
        Assert.Equal(new VehicleRevenueTotals(1, 2, 2000m), enero.Totals);
        Assert.Equal(new VehicleRevenueTotals(2, 4, 4000m), febrero.Totals);
    }

    [Fact]
    public async Task RF_REP_05_to_es_exclusivo()
    {
        // Arrange
        var corolla = Vehicle.Create(Ana, "Toyota", "Corolla", 2020, "A-100", 1000m, Publicado);
        var enElLimite = Reservation.Create(corolla.Id, Beto,
            new DateTime(2030, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2030, 2, 2, 0, 0, 0, DateTimeKind.Utc),
            1000m, Publicado);
        enElLimite.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, Publicado);
        var db = await BaseAsync([corolla], enElLimite, Reserva(corolla, Dia(2030, 1, 31), 1, ReservationStatus.Confirmed));

        // Act
        var report = await Handler(db, Admin, AccessControlRoles.Administrator)
            .Handle(new VehicleRevenueReportQuery("2030-01-01", "2030-02-01"), CancellationToken.None);

        // Assert
        Assert.Equal(1, report.Totals.Reservations);
    }

    [Theory]
    [InlineData(null, "2030-02-01", "from")]
    [InlineData("2030-01-01", "", "to")]
    [InlineData("2030/01/01", "2030-02-01", "from")]
    [InlineData("2030-02-01", "2030-01-01", "to")]
    public void RF_REP_05_rango_invalido_responde_400(string? from, string? to, string campo)
    {
        // Arrange
        var validator = new VehicleRevenueReportQueryValidator();

        // Act
        var result = validator.Validate(new VehicleRevenueReportQuery(from, to));

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == campo);
    }

    [Fact]
    public void RF_REP_04_el_reporte_de_ingresos_exige_sesion_y_acepta_los_dos_roles()
    {
        // Arrange
        // ISender registrado: así las rutas previas no lo infieren como cuerpo.
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddScoped<MediatR.ISender>(_ => throw new NotSupportedException());
        var app = builder.Build();
        app.MapWheelbyEndpoints();

        // Act
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(e => (e.RoutePattern.RawText ?? "").StartsWith("/reports", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Assert
        var unico = Assert.Single(endpoint);
        Assert.Equal("/reports/vehicle-revenue", unico.RoutePattern.RawText);
        var autorizaciones = unico.Metadata.OfType<AuthorizeAttribute>().ToList();
        Assert.NotEmpty(autorizaciones);
        Assert.All(autorizaciones, a => Assert.Null(a.Policy));
    }
}
