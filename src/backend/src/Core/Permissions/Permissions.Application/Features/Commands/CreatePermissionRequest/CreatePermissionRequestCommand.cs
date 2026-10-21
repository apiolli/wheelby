using Shared.Application.Abstractions;

namespace Permissions.Application.Features.CreatePermissionRequest;

public sealed record CreatePermissionRequestCommand(string PermissionCode) : ICommand<Guid>;
