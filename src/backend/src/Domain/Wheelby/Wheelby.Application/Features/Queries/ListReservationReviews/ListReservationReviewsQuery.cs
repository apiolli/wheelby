using Shared.Application.Abstractions;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.ListReservationReviews;

public sealed record ListReservationReviewsQuery(Guid ReservationId) : IQuery<IReadOnlyList<ReviewDTO>>;
