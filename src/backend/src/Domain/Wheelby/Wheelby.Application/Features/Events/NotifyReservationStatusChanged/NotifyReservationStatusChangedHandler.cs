using AccessControl.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Reservations.Events;
using INotificationPublisher = Notifications.Contracts.INotificationPublisher;

namespace Wheelby.Application.Features.NotifyReservationStatusChanged;

// RF-NEG-07 y RF-NOT-03: cada transición pasa por el motor de notificaciones, que deja
// el aviso en la bandeja y encola el correo (salvo que el destinatario lo desactivara).
// Avisa a la contraparte de quien actuó: si actúa el inquilino, al anfitrión, y al
// revés. Si actúa el sistema (expiración), a los dos. Es un efecto secundario: si
// falla, se registra y la operación del usuario no se revierte. Corre en la API y en
// el Worker, sin ICurrentUser.
internal sealed class NotifyReservationStatusChangedHandler(
    IReservationReadStore reservations,
    IUserDirectory users,
    INotificationPublisher notifications,
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

            // El nombre del inquilino va en el aviso de nueva solicitud al anfitrión.
            var renter = await users.GetContactAsync(reservation.RenterId, cancellationToken);
            var message = ReservationEmailTemplates.Build(change, reservation, renter?.FullName);
            var recipients = Recipients(change.Actor, reservation.RenterId, reservation.OwnerId).ToArray();

            await notifications.PublishAsync(message, recipients, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not notify reservation {ReservationId} ({From} -> {To}).",
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
