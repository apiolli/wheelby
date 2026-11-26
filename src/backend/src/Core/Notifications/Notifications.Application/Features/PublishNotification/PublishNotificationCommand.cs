using AccessControl.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Emails;
using Notifications.Domain.Inbox;
using Shared.Application.Abstractions;

namespace Notifications.Application.Features.PublishNotification;

// RF-NOT-06: una notificación por destinatario en la bandeja y, si tiene el correo
// activado (RF-NOT-07), un correo encolado con el mismo título como asunto.
// Todo se guarda con un solo SaveChanges: notificaciones y correos en una transacción.
// Devuelve cuántas notificaciones creó.
public sealed record PublishNotificationCommand(
    string Type,
    string Title,
    string Message,
    Guid? ReferenceId,
    IReadOnlyCollection<Guid> RecipientIds) : ICommand<int>;

internal sealed class PublishNotificationCommandHandler(
    INotificationRepository notifications,
    IQueuedEmailRepository emails,
    INotificationPreferenceRepository preferences,
    INotificationsUnitOfWork unitOfWork,
    IUserDirectory users,
    IClock clock,
    ILogger<PublishNotificationCommandHandler> logger) : IRequestHandler<PublishNotificationCommand, int>
{
    public const string EmailFooter = "Puedes ver tus notificaciones en Wheelby.";

    public async Task<int> Handle(PublishNotificationCommand request, CancellationToken cancellationToken)
    {
        var recipientIds = request.RecipientIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (recipientIds.Length == 0)
            return 0;

        var emailDisabled = await preferences.ListEmailDisabledAsync(recipientIds, cancellationToken);
        var utcNow = clock.UtcNow;
        var created = 0;

        foreach (var recipientId in recipientIds)
        {
            // null si no existe o está deshabilitado: no se le notifica.
            var contact = await users.GetContactAsync(recipientId, cancellationToken);
            if (contact is null)
            {
                logger.LogWarning("Notification {Type} skipped: recipient {RecipientId} not found or disabled.",
                    request.Type, recipientId);
                continue;
            }

            var notification = Notification.Create(
                recipientId, request.Type, request.Title, request.Message, request.ReferenceId, utcNow);

            if (!emailDisabled.Contains(recipientId))
            {
                var email = QueuedEmail.Create(
                    EmailAddress.Create(contact.Email), notification.Title, EmailBody(contact.FullName, notification.Message), utcNow);
                await emails.AddAsync(email, cancellationToken);
                notification.AttachEmail(email.Id);
            }

            await notifications.AddAsync(notification, cancellationToken);
            created++;
        }

        if (created > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        return created;
    }

    // Texto plano en español: saludo, el mensaje y una línea fija.
    internal static string EmailBody(string fullName, string message)
        => $"Hola, {fullName}:\n\n{message}\n\n{EmailFooter}\n\n— Wheelby";
}
