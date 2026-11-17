namespace Wheelby.Application.DTOs;

// Rangos bloqueados (Confirmed e InProgress) de un vehículo dentro de [From, To).
// No revela quién reservó ni el id de la reserva.
public sealed record VehicleAvailabilityDTO(
    Guid VehicleId,
    DateTime From,
    DateTime To,
    IReadOnlyList<BlockedRangeDTO> Blocked);

public sealed record BlockedRangeDTO(DateTime StartDate, DateTime EndDate);
