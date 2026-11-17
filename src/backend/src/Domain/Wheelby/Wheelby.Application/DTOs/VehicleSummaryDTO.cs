using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.DTOs;

// Los precios publicados están en pesos dominicanos (DOP). Si la búsqueda pide
// otra moneda, se completan los campos de conversión (RD-10).
public sealed record VehicleSummaryDTO(
    Guid Id,
    Guid OwnerId,
    string Brand,
    string Model,
    int Year,
    string Plate,
    decimal PricePerDay,
    DateTime CreatedAt,
    string Currency = "DOP",
    decimal? ConvertedPricePerDay = null,
    string? ConvertedCurrency = null,
    decimal? ExchangeRate = null,
    VehicleType Type = VehicleType.Car,
    string? City = null,
    string? Description = null,
    bool IsActive = true,
    // Primera foto del vehículo (P4), o null si no tiene: el cliente muestra la ilustración.
    Guid? CoverPhotoId = null);
