namespace Host.WebAPI.ErrorHandling;

// Códigos genéricos de problem+json (extensions.code) para los errores que no
// traen uno propio. Los módulos definen los suyos (p. ej. WheelbyErrorCodes).
internal static class ErrorCodes
{
    public const string Validation = "validation";
    public const string MalformedRequest = "request.malformed";
    public const string Internal = "internal";

    public static string ForStatus(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "bad-request",
        StatusCodes.Status401Unauthorized => "unauthorized",
        StatusCodes.Status403Forbidden => "forbidden",
        StatusCodes.Status404NotFound => "not-found",
        StatusCodes.Status409Conflict => "conflict",
        StatusCodes.Status503ServiceUnavailable => "service-unavailable",
        _ => "error"
    };
}
