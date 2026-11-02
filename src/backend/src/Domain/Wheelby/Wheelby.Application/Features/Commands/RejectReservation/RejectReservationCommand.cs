using Shared.Application.Abstractions;

namespace Wheelby.Application.Features.RejectReservation;

public sealed record RejectReservationCommand(Guid Id, string Reason) : ICommand;
