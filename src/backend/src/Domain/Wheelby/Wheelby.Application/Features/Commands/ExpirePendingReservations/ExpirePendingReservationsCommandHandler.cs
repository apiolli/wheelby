using MediatR;
using Shared.Application.Abstractions;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;

namespace Wheelby.Application.Features.ExpirePendingReservations;

// Confirm exige utcNow <= StartDate: una solicitud Pending cuyo inicio ya llegó
// no puede confirmarse y quedaría colgada. El sistema la pasa a Cancelled por la
// misma máquina de estados (actor System), con motivo y su fila de historial.
internal sealed class ExpirePendingReservationsCommandHandler(
    IReservationRepository reservations,
    IWheelbyUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<ExpirePendingReservationsCommand, int>, IWorkerHandler
{
    public const string Reason = "La solicitud expiró: llegó la fecha de inicio sin que el anfitrión la confirmara.";

    public async Task<int> Handle(ExpirePendingReservationsCommand command, CancellationToken cancellationToken)
    {
        var utcNow = clock.UtcNow;
        var expired = await reservations.ListExpiredPendingAsync(utcNow, command.BatchSize, cancellationToken);
        if (expired.Count == 0)
            return 0;

        foreach (var reservation in expired)
            reservation.TransitionTo(ReservationStatus.Cancelled, ReservationActor.System, utcNow, Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return expired.Count;
    }
}
