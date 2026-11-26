namespace Notifications.Domain.Inbox;

// RF-NOT-07: preferencia de canal de un usuario. Sin fila, el correo está activado.
// La bandeja interna no se puede desactivar; los correos de cuenta no la consultan.
public sealed class NotificationPreference
{
    public const bool DefaultEmailEnabled = true;

    public Guid UserId { get; private set; }
    public bool EmailEnabled { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private NotificationPreference() { }

    public static NotificationPreference Create(Guid userId, bool emailEnabled, DateTime utcNow)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("El usuario es obligatorio.", nameof(userId));

        return new NotificationPreference { UserId = userId, EmailEnabled = emailEnabled, UpdatedAt = utcNow };
    }

    public void SetEmailEnabled(bool emailEnabled, DateTime utcNow)
    {
        EmailEnabled = emailEnabled;
        UpdatedAt = utcNow;
    }
}
