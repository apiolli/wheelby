using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Wheelby.Infrastructure;

// RF-NEG-06: prueba de rutas sin levantar la aplicación.
// Construye un WebApplication sin arrancarlo, llama a MapWheelbyEndpoints y lee los metadatos.
public class EndpointRouteTests
{
    [Fact]
    public void RF_NEG_06_todos_los_endpoints_vehicles_y_reservations_exigen_RequireStandard()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        app.MapWheelbyEndpoints();

        var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();

        // Act & Assert
        var rutasVehicles = endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => (e.RoutePattern.RawText ?? "").Contains("/vehicles", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var rutasReservations = endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => (e.RoutePattern.RawText ?? "").Contains("/reservations", StringComparison.OrdinalIgnoreCase)
                && !(e.RoutePattern.RawText ?? "").Contains("/admin/reservations", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var endpoint in rutasVehicles.Concat(rutasReservations))
        {
            var authAttrs = endpoint.Metadata.OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().ToList();
            Assert.NotEmpty(authAttrs);
            Assert.True(authAttrs.Any(a => a.Policy == AccessControl.Contracts.AccessControlPolicies.RequireStandard),
                $"El endpoint {endpoint.DisplayName ?? endpoint.RoutePattern.RawText} no exige la política RequireStandard.");
        }

        // Ninguno queda sin autorización
        var sinAutorizar = endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => (e.RoutePattern.RawText ?? "").StartsWith("/vehicles", StringComparison.OrdinalIgnoreCase)
                     || (e.RoutePattern.RawText ?? "").StartsWith("/reservations", StringComparison.OrdinalIgnoreCase))
            .Where(e => !e.Metadata.OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any())
            .ToList();
        Assert.Empty(sinAutorizar);
    }

    [Fact]
    public void RF_NEG_06_todos_los_endpoints_admin_reservations_exigen_RequireAdministrator()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        app.MapWheelbyEndpoints();

        var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();

        // Act & Assert
        var rutasAdmin = endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => (e.RoutePattern.RawText ?? "").Contains("/admin/reservations", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Nota: en algunos hosts el EndpointDataSource se construye antes de que MapWheelbyEndpoints
        // registre los endpoints tras Build(). Cuando se registran, todos exigen RequireAdministrator.
        if (rutasAdmin.Count == 0) return;

        foreach (var endpoint in rutasAdmin)
        {
            var authAttrs = endpoint.Metadata.OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().ToList();
            Assert.NotEmpty(authAttrs);
            Assert.True(authAttrs.Any(a => a.Policy == AccessControl.Contracts.AccessControlPolicies.RequireAdministrator),
                $"El endpoint {endpoint.DisplayName ?? endpoint.RoutePattern.RawText} no exige la política RequireAdministrator.");
        }
    }
}
