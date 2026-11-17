using AccessControl.Contracts;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Errors;

namespace Wheelby.Application.Common;

// Quién ve una reserva y lo que cuelga de ella (historial, documentos, reseñas):
// el inquilino, el dueño del vehículo y el Administrador. Para cualquier otro la
// reserva "no existe" (404), así no se revela su existencia.
internal static class ReservationAccess
{
    public static async Task<ReservationSummaryDTO> GetVisibleAsync(
        IReservationReadStore store,
        ICurrentUser currentUser,
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        var reservation = await store.GetByIdAsync(reservationId, cancellationToken);

        var canSee = reservation is not null
            && (currentUser.Role == AccessControlRoles.Administrator
                || reservation.RenterId == currentUser.Id
                || reservation.OwnerId == currentUser.Id);

        if (!canSee)
            throw new NotFoundException("La reserva no existe.", WheelbyErrorCodes.ReservationNotFound);

        return reservation!;
    }
}
