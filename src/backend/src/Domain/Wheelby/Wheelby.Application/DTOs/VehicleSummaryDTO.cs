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
    decimal? ExchangeRate = null);
