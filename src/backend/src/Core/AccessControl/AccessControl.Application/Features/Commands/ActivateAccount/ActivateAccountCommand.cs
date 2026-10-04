using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.Activate;

public sealed record ActivateAccountCommand(Guid UserId, string Token) : ICommand;