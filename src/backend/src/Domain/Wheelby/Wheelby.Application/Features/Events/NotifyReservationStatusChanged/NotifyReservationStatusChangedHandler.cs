using AccessControl.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Notifications.Contracts;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Reservations.Events;

namespace Wheelby.Application.Features.NotifyReservationStatusChanged;

// RF-NEG-07: cada transición se convierte en un correo encolado; el Worker lo envía.
// Avisa a la contraparte de quien actuó: si actúa el inquilino, al anfitrión, y al
// revés. Si actúa el sistema (expiración), a los dos. El correo es un efecto
// secundario: si falla, se registra y la operación del usuario no se revierte.
internal sealed class NotifyReservationStatusChangedHandler(
    IReservationReadStore reservations,
    IUserDirectory users,
    IEmailQueue emailQueue,
    ILogger<NotifyReservationStatusChangedHandler> logger)
    : INotificationHandler<ReservationStatusChanged>, IWorkerHandler
{
    public async Task Handle(ReservationStatusChanged change, CancellationToken cancellationToken)
    {
        try
        {
            var reservation = await reservations.GetByIdAsync(change.ReservationId, cancellationToken);
            if (reservation is null)
            {
                logger.LogWarning("Reservation {ReservationId} not found while notifying.", change.ReservationId);
                return;
            }

            foreach (var recipientId in Recipients(change.Actor, reservation.RenterId, reservation.OwnerId))
            {
                var contact = await users.GetContactAsync(recipientId, cancellationToken);
                if (contact is null)
                    continue;

                var (subject, body) = ReservationEmailTemplates.Build(change, reservation, contact.FullName);
                await emailQueue.EnqueueAsync(contact.Email, subject, body, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not enqueue the email for reservation {ReservationId} ({From} -> {To}).",
                change.ReservationId, change.From, change.To);
        }
    }

    internal static IEnumerable<Guid> Recipients(ReservationActor actor, Guid renterId, Guid ownerId) => actor switch
    {
        ReservationActor.Renter => [ownerId],
        ReservationActor.Host => [renterId],
        _ => [renterId, ownerId]
    };
}
