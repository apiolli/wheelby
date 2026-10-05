using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.EnableUser;

public sealed record EnableUserCommand(Guid UserId) : ICommand;
