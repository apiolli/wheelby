using Shared.Application.Abstractions;

namespace Permissions.Application.Features.ApprovePermissionRequest;

public sealed record ApprovePermissionRequestCommand(Guid RequestId) : ICommand;
