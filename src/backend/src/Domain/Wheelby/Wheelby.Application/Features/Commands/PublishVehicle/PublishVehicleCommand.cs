using Shared.Application.Abstractions;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Features.PublishVehicle;

public sealed record PublishVehicleCommand(
    string Brand,
    string Model,
    int Year,
    string Plate,
    decimal PricePerDay,
    VehicleType? Type = null,
    string? City = null,
    string? Description = null) : ICommand<Guid>;
