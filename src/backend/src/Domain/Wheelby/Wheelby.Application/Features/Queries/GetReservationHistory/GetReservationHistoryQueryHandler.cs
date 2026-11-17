using AccessControl.Contracts;
using MediatR;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.GetReservationHistory;

// Línea de tiempo del detalle, en orden cronológico. Mismas reglas de visibilidad que el detalle.
internal sealed class GetReservationHistoryQueryHandler(
    IReservationReadStore reservations,
    ICurrentUser currentUser)
    : IRequestHandler<GetReservationHistoryQuery, IReadOnlyList<ReservationStatusChangeDTO>>
{
    public async Task<IReadOnlyList<ReservationStatusChangeDTO>> Handle(
        GetReservationHistoryQuery query,
        CancellationToken cancellationToken)
    {
        await ReservationAccess.GetVisibleAsync(reservations, currentUser, query.ReservationId, cancellationToken);
        return await reservations.ListHistoryAsync(query.ReservationId, cancellationToken);
    }
}
