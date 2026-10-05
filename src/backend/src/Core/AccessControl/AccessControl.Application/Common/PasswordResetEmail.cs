namespace AccessControl.Application.Common;

internal static class PasswordResetEmail
{
    public const string Subject = "Recupera tu contraseña de Wheelby";

    public static string BuildBody(string fullName, string code, TimeSpan lifetime, bool forcedByAdministrator) => $"""
        Hola {fullName},

        {(forcedByAdministrator
            ? "Un administrador restableció tu contraseña y la anterior ya no sirve. Usa este código para definir una nueva:"
            : "Recibimos una solicitud para restablecer tu contraseña. Usa este código:")}

        {code}

        Es válido por {(int)lifetime.TotalMinutes} minutos. Para restablecerla envía POST /auth/reset-password con tu correo, el código y la contraseña nueva.

        {(forcedByAdministrator ? "" : "Si no lo solicitaste, ignora este mensaje.")}
        """;
}
