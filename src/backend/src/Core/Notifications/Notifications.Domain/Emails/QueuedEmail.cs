using Notifications.Domain.Emails.Enums;
using Notifications.Domain.Emails.Exceptions;
using SharedKernel.Domain;
using SharedKernel.Exceptions;

namespace Notifications.Domain.Emails;

public sealed class QueuedEmail : Entity<Guid>
{
    public const int SubjectMaxLength = 200;
    public const int LastErrorMaxLength = 500;

    public EmailAddress To { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public EmailStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    // RF-NOT-10: intentos hechos (exitoso o no), fecha y error del último, y cuándo quedó fallido.
    public int Attempts { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? FailedAt { get; private set; }

    // Constructor privado para EF Core
    private QueuedEmail() : base(Guid.Empty) { }

    private QueuedEmail(Guid id, EmailAddress to, string subject, string body, DateTime utcNow)
        : base(id)
    {
        To = to;
        Subject = subject;
        Body = body;
        Status = EmailStatus.Pending;
        CreatedAt = utcNow;
    }

    public static QueuedEmail Create(EmailAddress to, string? subject, string? body, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(to);

        var cleanSubject = subject?.Trim();
        if (string.IsNullOrEmpty(cleanSubject))
            throw new InvalidEmailDataException("El asunto del correo es obligatorio.");
            
        if (cleanSubject.Length > SubjectMaxLength)
            throw new InvalidEmailDataException("El asunto del correo es demasiado largo.");

        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidEmailDataException("El cuerpo del correo es obligatorio.");

        return new QueuedEmail(Guid.NewGuid(), to, cleanSubject, body, utcNow);
    }

    // El envío exitoso también cuenta como intento: si salió a la primera, Attempts = 1.
    public void MarkAsSent(DateTime utcNow)
    {
        EnsurePending();

        Attempts++;
        LastAttemptAt = utcNow;
        Status = EmailStatus.Sent;
        SentAt = utcNow;
    }

    // Suma un intento fallido y guarda el error recortado. Al llegar al máximo
    // queda Failed y ya no se reintenta; el correo no se borra.
    public void RegisterFailure(string? error, DateTime utcNow, int maxAttempts)
    {
        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "El máximo de intentos debe ser 1 o mayor.");

        EnsurePending();

        Attempts++;
        LastAttemptAt = utcNow;
        LastError = Truncate(string.IsNullOrWhiteSpace(error) ? "Error desconocido al enviar." : error.Trim());

        if (Attempts >= maxAttempts)
        {
            Status = EmailStatus.Failed;
            FailedAt = utcNow;
        }
    }

    private void EnsurePending()
    {
        if (Status == EmailStatus.Sent)
            throw new ConflictException("El correo ya fue enviado.");

        if (Status == EmailStatus.Failed)
            throw new ConflictException("El correo ya agotó sus intentos de envío.");
    }

    private static string Truncate(string value)
        => value.Length <= LastErrorMaxLength ? value : value[..LastErrorMaxLength];
}
