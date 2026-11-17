using Shared.Application.Abstractions;

namespace Wheelby.Application.Features.SetVehicleActive;

public sealed record SetVehicleActiveCommand(Guid Id, bool IsActive) : ICommand;
