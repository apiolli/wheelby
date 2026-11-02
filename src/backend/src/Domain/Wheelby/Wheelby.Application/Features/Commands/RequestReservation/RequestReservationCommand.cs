using Shared.Application.Abstractions;

namespace Wheelby.Application.Features.RequestReservation;

public sealed record RequestReservationCommand(
    Guid VehicleId,
    DateTime Start,
    DateTime End) : ICommand<Guid>;
