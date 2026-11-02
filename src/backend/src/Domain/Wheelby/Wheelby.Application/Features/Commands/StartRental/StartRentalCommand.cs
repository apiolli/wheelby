using Shared.Application.Abstractions;

namespace Wheelby.Application.Features.StartRental;

public sealed record StartRentalCommand(Guid Id) : ICommand;
