using Wheelby.Domain.Reservations;

namespace Wheelby.Application.DTOs;

// Resumen de reserva con los datos del vehículo, para que el cliente pinte
// la tarjeta sin un segundo fetch. PricePerDay y TotalAmount son los congelados
// al reservar, en DOP; no cambian si el anfitrión edita el precio después.
public sealed record ReservationSummaryDTO(
    Guid Id,
    Guid VehicleId,
    Guid RenterId,
    Guid OwnerId,
    string Brand,
    string Model,
    int Year,
    string Plate,
    DateTime StartDate,
    DateTime EndDate,
    ReservationStatus Status,
    decimal PricePerDay,
    int Days,
    decimal TotalAmount,
    string? CancellationReason,
    DateTime CreatedAt,
    string Currency = "DOP",
    // Primera foto del vehículo, o null.
    Guid? CoverPhotoId = null);
