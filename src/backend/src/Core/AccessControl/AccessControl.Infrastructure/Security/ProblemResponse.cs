using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AccessControl.Infrastructure.Security;

// Cuerpo de los 401 y 403 que genera la autenticación, fuera del alcance del manejador de excepciones.
internal static class ProblemResponse
{
    public static Task WriteAsync(HttpContext httpContext, int status, string detail)
    {
        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = detail
        };
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return httpContext.Response.WriteAsJsonAsync(
            problem, problem.GetType(), options: null,
            contentType: "application/problem+json", httpContext.RequestAborted);
    }
}