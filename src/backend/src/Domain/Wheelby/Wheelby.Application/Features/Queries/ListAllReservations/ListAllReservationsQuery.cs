using Shared.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Reservations;

namespace Wheelby.Application.Features.ListAllReservations;

public sealed record ListAllReservationsQuery(
    ReservationStatus? Status,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ReservationSummaryDTO>>;
