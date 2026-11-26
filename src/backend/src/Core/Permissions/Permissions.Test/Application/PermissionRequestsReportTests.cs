using AccessControl.Contracts;
using Permissions.Application.Features.PermissionRequestsReport;
using Permissions.Domain.PermissionRequests;
using Permissions.Infrastructure.Persistence;
using Permissions.Test.Support;

namespace Permissions.Test.Application;

// RF-REP-01 a 05: reporte de solicitudes de permiso por mes y estado.
public sealed class PermissionRequestsReportTests
{
    private static readonly Guid Ana = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Beto = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Admin = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static DateTime Utc(int year, int month, int day, int hour = 12)
        => new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    private static PermissionRequest Pendiente(Guid requester, DateTime createdAt, string code = "core.demo-action")
        => PermissionRequest.Create(requester, code, createdAt);

    private static PermissionRequest Aplicada(Guid requester, DateTime createdAt, string code = "core.demo-action")
    {
        var r = PermissionRequest.Create(requester, code, createdAt);
        r.Approve(Admin, createdAt.AddHours(1));
        r.MarkApplied(createdAt.AddHours(1));
        return r;
    }

    private static PermissionRequest Rechazada(Guid requester, DateTime createdAt, string code = "core.demo-action")
    {
        var r = PermissionRequest.Create(requester, code, createdAt);
        r.Reject(Admin, createdAt.AddHours(1), "No corresponde");
        return r;
    }

    private static async Task<PermissionRequestsReportQueryHandler> HandlerAsync(
        Guid userId, string role, params PermissionRequest[] seed)
    {
        var db = PermissionsDb.CreaContexto();
        db.PermissionRequests.AddRange(seed);
        await db.SaveChangesAsync();
        return new PermissionRequestsReportQueryHandler(new PermissionRequestReportStore(db), new FixedCurrentUser(userId, role));
    }

    [Fact]
    public async Task RF_REP_01_agrupa_solicitudes_por_mes_y_estado()
    {
        // Arrange
        var handler = await HandlerAsync(Admin, AccessControlRoles.Administrator,
            Pendiente(Ana, Utc(2026, 3, 10)),
            Rechazada(Beto, Utc(2026, 3, 11), "core.otro"),
            Aplicada(Ana, Utc(2026, 5, 2), "core.tres"),
            Pendiente(Beto, Utc(2026, 5, 20), "core.cuatro"));

        // Act
        var report = await handler.Handle(new PermissionRequestsReportQuery("2026-01-01", "2027-01-01"), CancellationToken.None);

        // Assert
        Assert.Equal("all", report.Scope);
        Assert.Equal(new DateOnly(2026, 1, 1), report.From);
        Assert.Equal(new DateOnly(2027, 1, 1), report.To);
        Assert.Equal(4, report.Total);
        Assert.Equal(
            new[]
            {
                new PermissionRequestsReportRow("2026-03", "Pending", 1),
                new PermissionRequestsReportRow("2026-03", "Rejected", 1),
                new PermissionRequestsReportRow("2026-05", "Applied", 1),
                new PermissionRequestsReportRow("2026-05", "Pending", 1),
            },
            report.Rows);
    }

    [Fact]
    public async Task RF_REP_03_varios_registros_producen_una_sola_fila()
    {
        // Arrange
        var handler = await HandlerAsync(Admin, AccessControlRoles.Administrator,
            Pendiente(Ana, Utc(2026, 4, 1)),
            Pendiente(Beto, Utc(2026, 4, 15)),
            Pendiente(Admin, Utc(2026, 4, 30)));

        // Act
        var report = await handler.Handle(new PermissionRequestsReportQuery("2026-04-01", "2026-05-01"), CancellationToken.None);

        // Assert
        var fila = Assert.Single(report.Rows);
        Assert.Equal(new PermissionRequestsReportRow("2026-04", "Pending", 3), fila);
        Assert.Equal(3, report.Total);
    }

    [Fact]
    public async Task RF_REP_04_estandar_solo_cuenta_sus_solicitudes()
    {
        // Arrange
        var seed = new[]
        {
            Pendiente(Ana, Utc(2026, 6, 1)),
            Rechazada(Ana, Utc(2026, 6, 2), "core.otro"),
            Pendiente(Beto, Utc(2026, 6, 3)),
        };
        var db = PermissionsDb.CreaContexto();
        db.PermissionRequests.AddRange(seed);
        await db.SaveChangesAsync();
        var store = new PermissionRequestReportStore(db);
        var query = new PermissionRequestsReportQuery("2026-06-01", "2026-07-01");

        // Act
        var deAna = await new PermissionRequestsReportQueryHandler(store, new FixedCurrentUser(Ana, AccessControlRoles.Standard))
            .Handle(query, CancellationToken.None);
        var deBeto = await new PermissionRequestsReportQueryHandler(store, new FixedCurrentUser(Beto, AccessControlRoles.Standard))
            .Handle(query, CancellationToken.None);
        var delAdmin = await new PermissionRequestsReportQueryHandler(store, new FixedCurrentUser(Admin, AccessControlRoles.Administrator))
            .Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal("own", deAna.Scope);
        Assert.Equal(2, deAna.Total);
        Assert.Equal(1, deBeto.Total);
        Assert.Equal("all", delAdmin.Scope);
        Assert.Equal(3, delAdmin.Total);
    }

    [Fact]
    public async Task RF_REP_05_dos_rangos_distintos_dan_resultados_distintos()
    {
        // Arrange
        var handler = await HandlerAsync(Admin, AccessControlRoles.Administrator,
            Pendiente(Ana, Utc(2030, 1, 10)),
            Aplicada(Beto, Utc(2030, 2, 10)),
            Rechazada(Ana, Utc(2030, 2, 11), "core.otro"));

        // Act
        var enero = await handler.Handle(new PermissionRequestsReportQuery("2030-01-01", "2030-02-01"), CancellationToken.None);
        var febrero = await handler.Handle(new PermissionRequestsReportQuery("2030-02-01", "2030-03-01"), CancellationToken.None);

        // Assert
        Assert.Equal(new[] { new PermissionRequestsReportRow("2030-01", "Pending", 1) }, enero.Rows);
        Assert.Equal(
            new[] { new PermissionRequestsReportRow("2030-02", "Applied", 1), new PermissionRequestsReportRow("2030-02", "Rejected", 1) },
            febrero.Rows);
    }

    [Fact]
    public async Task RF_REP_05_to_es_exclusivo()
    {
        // Arrange
        var handler = await HandlerAsync(Admin, AccessControlRoles.Administrator,
            Pendiente(Ana, new DateTime(2030, 1, 31, 23, 59, 59, DateTimeKind.Utc)),
            Pendiente(Beto, new DateTime(2030, 2, 1, 0, 0, 0, DateTimeKind.Utc)));

        // Act
        var report = await handler.Handle(new PermissionRequestsReportQuery("2030-01-01", "2030-02-01"), CancellationToken.None);

        // Assert
        Assert.Equal(1, report.Total);
        Assert.Equal("2030-01", Assert.Single(report.Rows).Period);
    }

    [Theory]
    [InlineData(null, "2026-02-01", "from")]
    [InlineData("2026-01-01", null, "to")]
    [InlineData("01/01/2026", "2026-02-01", "from")]
    [InlineData("2026-01-01", "2026-13-01", "to")]
    [InlineData("2026-02-01", "2026-02-01", "to")]
    [InlineData("2026-03-01", "2026-02-01", "to")]
    public void RF_REP_05_rango_invalido_responde_400(string? from, string? to, string campo)
    {
        // Arrange
        var validator = new PermissionRequestsReportQueryValidator();

        // Act
        var result = validator.Validate(new PermissionRequestsReportQuery(from, to));

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == campo);
    }
}
