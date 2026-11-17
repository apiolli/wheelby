using Shared.Application.Abstractions;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Features.UpdateVehicle;

// PUT: reemplaza todos los campos editables. Id viene de la ruta.
public sealed record UpdateVehicleCommand(
    Guid Id,
    string Brand,
    string Model,
    int Year,
    string Plate,
    decimal PricePerDay,
    VehicleType Type,
    string? City,
    string? Description) : ICommand;
