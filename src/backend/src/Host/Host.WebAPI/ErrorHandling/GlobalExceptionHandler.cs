using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using SharedKernel.Exceptions;

namespace Host.WebAPI.ErrorHandling;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException validation => CreateValidationProblem(validation),
            AppException app => CreateAppProblem(app),
            BadHttpRequestException => CreateBadRequestProblem(exception),
            _ => CreateUnexpectedProblem(exception, httpContext)
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(
            problem, problem.GetType(), options: null,
            contentType: "application/problem+json", cancellationToken);

        return true;
    }

    // 400
    private ProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        logger.LogWarning("Request rejected (400): validation failed for {Fields}",
            string.Join(", ", errors.Keys));

        return new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest),
            Detail = "Uno o más campos no son válidos."
        };
    }

    // 401, 403, 404, 409...
    private ProblemDetails CreateAppProblem(AppException exception)
    {
        logger.LogWarning("Request rejected ({Status}): {Message}",
            exception.StatusCode, exception.Message);

        return new ProblemDetails
        {
            Status = exception.StatusCode,
            Title = ReasonPhrases.GetReasonPhrase(exception.StatusCode),
            Detail = exception.Message
        };
    }

    // 500
    private ProblemDetails CreateUnexpectedProblem(Exception exception, HttpContext httpContext)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);

        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError),
            Detail = "Ocurrió un error interno del servidor."
        };
    }

    // 400: el JSON está roto o un parámetro no tiene el tipo esperado
    private ProblemDetails CreateBadRequestProblem(Exception exception)
    {
        logger.LogWarning("Request rejected (400): malformed request ({Reason})", exception.GetType().Name);

        return new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest),
            Detail = "La solicitud no tiene un formato válido."
        };
    }
}