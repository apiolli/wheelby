using Shared.Application.Abstractions;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.GetReservationHistory;

public sealed record GetReservationHistoryQuery(Guid ReservationId) : IQuery<IReadOnlyList<ReservationStatusChangeDTO>>;
