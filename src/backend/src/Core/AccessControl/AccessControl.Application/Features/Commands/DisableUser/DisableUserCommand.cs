using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.DisableUser;

public sealed record DisableUserCommand(Guid UserId) : ICommand;
