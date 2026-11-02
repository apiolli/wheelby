using AccessControl.Contracts;
using MediatR;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.ListMyReservations;

internal sealed class ListMyReservationsQueryHandler(
    IReservationReadStore reservationReadStore,
    ICurrentUser currentUser)
    : IRequestHandler<ListMyReservationsQuery, PagedResult<ReservationSummaryDTO>>
{
    public Task<PagedResult<ReservationSummaryDTO>> Handle(
        ListMyReservationsQuery query,
        CancellationToken cancellationToken)
    {
        return query.As switch
        {
            "renter" => reservationReadStore.ListByRenterAsync(currentUser.Id, query.Page, query.PageSize, cancellationToken),
            "host" => reservationReadStore.ListByHostAsync(currentUser.Id, query.Page, query.PageSize, cancellationToken),
            _ => throw new InvalidOperationException($"El valor '{query.As}' no es válido para 'as'.")
        };
    }
}
