using Shared.Application.Abstractions;

namespace Wheelby.Application.Features.CancelReservation;

public sealed record CancelReservationCommand(Guid Id, string? Reason) : ICommand;
