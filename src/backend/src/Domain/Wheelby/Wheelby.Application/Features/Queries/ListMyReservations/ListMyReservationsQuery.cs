using Shared.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.ListMyReservations;

public sealed record ListMyReservationsQuery(
    string As,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ReservationSummaryDTO>>;
