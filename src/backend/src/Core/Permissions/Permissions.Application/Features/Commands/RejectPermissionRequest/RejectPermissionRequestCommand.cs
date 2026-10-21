using Shared.Application.Abstractions;

namespace Permissions.Application.Features.RejectPermissionRequest;

public sealed record RejectPermissionRequestCommand(Guid RequestId, string? Reason) : ICommand;
