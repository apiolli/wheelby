using Shared.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.ListVehicleReviews;

public sealed record ListVehicleReviewsQuery(
    Guid VehicleId,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ReviewDTO>>;
