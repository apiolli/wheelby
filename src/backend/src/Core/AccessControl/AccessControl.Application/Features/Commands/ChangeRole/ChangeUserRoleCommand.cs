using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.ChangeRole;

public sealed record ChangeUserRoleCommand(Guid UserId, string Role) : ICommand;
