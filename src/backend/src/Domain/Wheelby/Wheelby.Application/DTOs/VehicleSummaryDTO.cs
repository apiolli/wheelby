namespace Wheelby.Application.DTOs;

public sealed record VehicleSummaryDTO(
    Guid Id,
    Guid OwnerId,
    string Brand,
    string Model,
    int Year,
    string Plate,
    decimal PricePerDay,
    DateTime CreatedAt);
