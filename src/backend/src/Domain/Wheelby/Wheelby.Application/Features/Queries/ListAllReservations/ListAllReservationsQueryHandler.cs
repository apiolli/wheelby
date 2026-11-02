using MediatR;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.ListAllReservations;

internal sealed class ListAllReservationsQueryHandler(
    IReservationReadStore reservationReadStore)
    : IRequestHandler<ListAllReservationsQuery, PagedResult<ReservationSummaryDTO>>
{
    public Task<PagedResult<ReservationSummaryDTO>> Handle(
        ListAllReservationsQuery query,
        CancellationToken cancellationToken)
        => reservationReadStore.ListAllAsync(query.Status, query.Page, query.PageSize, cancellationToken);
}
