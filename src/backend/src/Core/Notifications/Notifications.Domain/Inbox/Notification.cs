using Notifications.Domain.Inbox.Exceptions;
using SharedKernel.Domain;

namespace Notifications.Domain.Inbox;

// RF-NOT-04: entrada de la bandeja interna de un usuario. Si el destinatario tiene el
// correo activado, EmailId apunta al correo encolado con el mismo título (RF-NOT-06).
public sealed class Notification : Entity<Guid>
{
    public const int TypeMaxLength = 100;
    public const int TitleMaxLength = 200;
    public const int MessageMaxLength = 4000;

    public Guid RecipientId { get; private set; }
    public string Type { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public Guid? ReferenceId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public Guid? EmailId { get; private set; }

    public bool IsRead => ReadAt is not null;

    private Notification() : base(Guid.Empty) { }

    private Notification(
        Guid id, Guid recipientId, string type, string title, string message, Guid? referenceId, DateTime utcNow)
        : base(id)
    {
        RecipientId = recipientId;
        Type = type;
        Title = title;
        Message = message;
        ReferenceId = referenceId;
        CreatedAt = utcNow;
    }

    public static Notification Create(
        Guid recipientId, string? type, string? title, string? message, Guid? referenceId, DateTime utcNow)
    {
        if (recipientId == Guid.Empty)
            throw new InvalidNotificationDataException("El destinatario es obligatorio.");

        var cleanType = type?.Trim();
        if (string.IsNullOrEmpty(cleanType) || cleanType.Length > TypeMaxLength)
            throw new InvalidNotificationDataException($"El tipo es obligatorio y tiene como máximo {TypeMaxLength} caracteres.");

        var cleanTitle = title?.Trim();
        if (string.IsNullOrEmpty(cleanTitle) || cleanTitle.Length > TitleMaxLength)
            throw new InvalidNotificationDataException($"El título es obligatorio y tiene como máximo {TitleMaxLength} caracteres.");

        var cleanMessage = message?.Trim();
        if (string.IsNullOrEmpty(cleanMessage) || cleanMessage.Length > MessageMaxLength)
            throw new InvalidNotificationDataException($"El mensaje es obligatorio y tiene como máximo {MessageMaxLength} caracteres.");

        if (utcNow.Kind != DateTimeKind.Utc)
            throw new InvalidNotificationDataException("La fecha debe estar en UTC.");

        return new Notification(Guid.NewGuid(), recipientId, cleanType, cleanTitle, cleanMessage, referenceId, utcNow);
    }

    // RF-NOT-06: enlaza el correo encolado para el mismo aviso.
    public void AttachEmail(Guid emailId)
    {
        if (emailId == Guid.Empty)
            throw new InvalidNotificationDataException("El correo es obligatorio.");

        EmailId = emailId;
    }

    // RF-NOT-05: idempotente. Si ya está leída, conserva la primera fecha.
    public void MarkAsRead(DateTime utcNow)
    {
        if (IsRead)
            return;

        ReadAt = utcNow;
    }
}
