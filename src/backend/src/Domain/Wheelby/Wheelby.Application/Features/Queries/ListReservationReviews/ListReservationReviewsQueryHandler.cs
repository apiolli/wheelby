using AccessControl.Contracts;
using MediatR;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.ListReservationReviews;

internal sealed class ListReservationReviewsQueryHandler(
    IReservationReadStore reservations,
    ICurrentUser currentUser)
    : IRequestHandler<ListReservationReviewsQuery, IReadOnlyList<ReviewDTO>>
{
    public async Task<IReadOnlyList<ReviewDTO>> Handle(ListReservationReviewsQuery query, CancellationToken cancellationToken)
    {
        await ReservationAccess.GetVisibleAsync(reservations, currentUser, query.ReservationId, cancellationToken);
        return await reservations.ListReviewsAsync(query.ReservationId, cancellationToken);
    }
}
