using AccessControl.Contracts;
using MediatR;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Errors;

namespace Wheelby.Application.Features.GetReservation;

// Ven el detalle el inquilino, el dueño del vehículo y el Administrador.
// Para cualquier otro la reserva "no existe" (404): no se revela su existencia.
internal sealed class GetReservationQueryHandler(
    IReservationReadStore reservationReadStore,
    ICurrentUser currentUser)
    : IRequestHandler<GetReservationQuery, ReservationSummaryDTO>
{
    public async Task<ReservationSummaryDTO> Handle(
        GetReservationQuery query,
        CancellationToken cancellationToken)
    {
        var reservation = await reservationReadStore.GetByIdAsync(query.Id, cancellationToken);

        var canSee = reservation is not null
            && (currentUser.Role == AccessControlRoles.Administrator
                || reservation.RenterId == currentUser.Id
                || reservation.OwnerId == currentUser.Id);

        if (!canSee)
            throw new NotFoundException("La reserva no existe.", WheelbyErrorCodes.ReservationNotFound);

        return reservation!;
    }
}
