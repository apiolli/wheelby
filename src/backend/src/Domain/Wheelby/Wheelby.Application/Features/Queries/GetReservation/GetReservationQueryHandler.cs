using AccessControl.Contracts;
using MediatR;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.GetReservation;

// Ven el detalle el inquilino, el dueño del vehículo y el Administrador; para otro, 404.
internal sealed class GetReservationQueryHandler(
    IReservationReadStore reservationReadStore,
    ICurrentUser currentUser)
    : IRequestHandler<GetReservationQuery, ReservationSummaryDTO>
{
    public Task<ReservationSummaryDTO> Handle(GetReservationQuery query, CancellationToken cancellationToken)
        => ReservationAccess.GetVisibleAsync(reservationReadStore, currentUser, query.Id, cancellationToken);
}
