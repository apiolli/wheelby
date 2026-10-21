using Permissions.Domain.PermissionRequests;

namespace Permissions.Application.Abstractions;

public interface IPermissionRequestRepository
{
    Task<PermissionRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsPendingAsync(
        Guid requesterId,
        string permissionCode,
        CancellationToken cancellationToken);

    Task AddAsync(PermissionRequest request, CancellationToken cancellationToken);
}
