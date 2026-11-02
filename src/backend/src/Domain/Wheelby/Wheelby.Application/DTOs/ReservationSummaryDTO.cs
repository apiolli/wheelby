using Wheelby.Domain.Reservations;

namespace Wheelby.Application.DTOs;

public sealed record ReservationSummaryDTO(
    Guid Id,
    Guid VehicleId,
    Guid RenterId,
    DateTime StartDate,
    DateTime EndDate,
    ReservationStatus Status,
    DateTime CreatedAt);
