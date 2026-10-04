namespace AccessControl.Application.Common;

internal static class ActivationEmail
{
    public const string Subject = "Activa tu cuenta de Wheelby";

    public static string BuildBody(string fullName, string link, TimeSpan lifetime) => $"""
        Hola {fullName},

        Para activar tu cuenta abre este enlace (válido por {(int)lifetime.TotalHours} horas):

        {link}

        Si no creaste esta cuenta, ignora este mensaje.
        """;
}