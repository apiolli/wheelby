using Shared.Application.Abstractions;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.GetReservation;

public sealed record GetReservationQuery(Guid Id) : IQuery<ReservationSummaryDTO>;
