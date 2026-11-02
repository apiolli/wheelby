using Shared.Application.Abstractions;

namespace Wheelby.Application.Features.ConfirmReservation;

public sealed record ConfirmReservationCommand(Guid Id) : ICommand;
