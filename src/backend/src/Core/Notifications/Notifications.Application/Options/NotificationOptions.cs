namespace Notifications.Application.Options;

// Sección Notifications de la configuración (variables Notifications__*).
public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";
    public const int DefaultMaxEmailAttempts = 5;

    // RF-NOT-10: intentos de envío antes de dejar el correo en Failed. Mínimo 1.
    public int MaxEmailAttempts { get; set; } = DefaultMaxEmailAttempts;
}
