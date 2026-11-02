using Shared.Application.Abstractions;

namespace Wheelby.Application.Features.CompleteRental;

public sealed record CompleteRentalCommand(Guid Id) : ICommand;
