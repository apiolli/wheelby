using Shared.Application.Abstractions;

namespace Wheelby.Application.Features.PublishVehicle;

public sealed record PublishVehicleCommand(
    string Brand,
    string Model,
    int Year,
    string Plate,
    decimal PricePerDay) : ICommand<Guid>;
