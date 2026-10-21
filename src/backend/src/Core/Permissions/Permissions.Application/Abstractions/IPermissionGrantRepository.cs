using Permissions.Domain.PermissionGrants;

namespace Permissions.Application.Abstractions;

public interface IPermissionGrantRepository
{
    Task<bool> ExistsAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken);

    Task AddAsync(PermissionGrant grant, CancellationToken cancellationToken);
}
