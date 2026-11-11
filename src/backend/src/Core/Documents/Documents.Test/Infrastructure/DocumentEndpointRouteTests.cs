using Documents.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Documents.Test.Infrastructure;

// RD-06: prueba de rutas sin levantar la aplicación. Construye un WebApplication
// sin arrancarlo, llama a MapDocumentsEndpoints y lee los metadatos de cada endpoint.
public class DocumentEndpointRouteTests
{
    private static List<RouteEndpoint> DocumentEndpoints()
    {
        var app = WebApplication.CreateBuilder().Build();
        app.MapDocumentsEndpoints();

        // Sin arrancar la app, los endpoints se leen de las DataSources del propio app:
        // el EndpointDataSource del contenedor todavía está vacío.
        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(e => (e.RoutePattern.RawText ?? "").StartsWith("/documents", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    [Fact]
    public void RD_06_Hay_cuatro_endpoints_de_documentos()
    {
        // Arrange (en DocumentEndpoints)

        // Act
        var endpoints = DocumentEndpoints();

        // Assert
        var routes = endpoints
            .Select(e => $"{e.Metadata.OfType<HttpMethodMetadata>().Single().HttpMethods.Single()} {e.RoutePattern.RawText!.TrimEnd('/')}")
            .OrderBy(r => r)
            .ToList();
        Assert.Equal(
            new[]
            {
                "DELETE /documents/{id:guid}",
                "GET /documents",
                "GET /documents/{id:guid}/content",
                "POST /documents",
            },
            routes);
    }

    [Fact]
    public void RD_06_Los_cuatro_endpoints_de_documentos_exigen_sesion()
    {
        // Arrange (en DocumentEndpoints)

        // Act
        var endpoints = DocumentEndpoints();
        var sinAutorizar = endpoints
            .Where(e => !e.Metadata.OfType<AuthorizeAttribute>().Any())
            .Select(e => e.RoutePattern.RawText)
            .ToList();

        // Assert
        Assert.Equal(4, endpoints.Count);
        Assert.Empty(sinAutorizar);
    }
}
