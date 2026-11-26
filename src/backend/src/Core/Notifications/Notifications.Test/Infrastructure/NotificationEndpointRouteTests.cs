using AccessControl.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Notifications.Infrastructure;

namespace Notifications.Test.Infrastructure;

// RD-06: prueba de rutas sin levantar la aplicación. Se leen las DataSources del
// propio app (el EndpointDataSource del contenedor sigue vacío sin arrancar).
public sealed class NotificationEndpointRouteTests
{
    private static List<RouteEndpoint> Endpoints(string prefix)
    {
        var app = WebApplication.CreateBuilder().Build();
        app.MapNotificationsEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(e => (e.RoutePattern.RawText ?? "").StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string Route(RouteEndpoint e)
        => $"{e.Metadata.OfType<HttpMethodMetadata>().Single().HttpMethods.Single()} {e.RoutePattern.RawText!.TrimEnd('/')}";

    [Fact]
    public void RF_NOT_04_rutas_de_notificaciones_exigen_sesion()
    {
        // Arrange (en Endpoints)

        // Act
        var endpoints = Endpoints("/notifications");

        // Assert
        Assert.Equal(
            new[]
            {
                "GET /notifications",
                "GET /notifications/preferences",
                "POST /notifications/{id:guid}/read",
                "PUT /notifications/preferences",
            },
            endpoints.Select(Route).OrderBy(r => r, StringComparer.Ordinal));
        Assert.All(endpoints, e => Assert.NotEmpty(e.Metadata.OfType<AuthorizeAttribute>()));
    }

    [Fact]
    public void RF_NOT_11_rutas_de_la_cola_exigen_administrador()
    {
        // Arrange (en Endpoints)

        // Act
        var endpoints = Endpoints("/admin/notifications");

        // Assert
        Assert.Equal(
            new[] { "GET /admin/notifications/emails", "GET /admin/notifications/emails/{id:guid}" },
            endpoints.Select(Route).OrderBy(r => r));
        Assert.All(endpoints, e => Assert.Contains(
            e.Metadata.OfType<AuthorizeAttribute>(),
            a => a.Policy == AccessControlPolicies.RequireAdministrator));
    }
}
