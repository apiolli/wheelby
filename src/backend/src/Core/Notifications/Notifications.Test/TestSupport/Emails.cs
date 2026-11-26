using Notifications.Domain.Emails;

namespace Notifications.Test.TestSupport;

// Fábrica de correos de prueba con datos fijos.
public static class Emails
{
    public static QueuedEmail Pendiente(string to, DateTime createdAt)
        => QueuedEmail.Create(EmailAddress.Create(to), "Asunto de prueba", "Cuerpo de prueba", createdAt);
}
