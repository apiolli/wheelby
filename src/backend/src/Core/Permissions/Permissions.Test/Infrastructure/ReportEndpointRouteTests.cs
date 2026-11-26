using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Permissions.Infrastructure;

namespace Permissions.Test.Infrastructure;

// RD-06: el reporte exige sesión y acepta los dos roles (sin política de rol en el grupo).
public sealed class ReportEndpointRouteTests
{
    [Fact]
    public void RF_REP_04_el_reporte_de_solicitudes_exige_sesion_y_acepta_los_dos_roles()
    {
        // Arrange
        // ISender registrado: así las rutas previas no lo infieren como cuerpo.
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddScoped<ISender>(_ => throw new NotSupportedException());
        var app = builder.Build();
        app.MapPermissionsEndpoints();

        // Act
        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(e => (e.RoutePattern.RawText ?? "").StartsWith("/reports", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Assert
        var endpoint = Assert.Single(endpoints);
        Assert.Equal("/reports/permission-requests", endpoint.RoutePattern.RawText);
        var autorizaciones = endpoint.Metadata.OfType<AuthorizeAttribute>().ToList();
        Assert.NotEmpty(autorizaciones);
        Assert.All(autorizaciones, a => Assert.Null(a.Policy));
    }
}
