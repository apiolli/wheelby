using Microsoft.EntityFrameworkCore;
using Permissions.Contracts;
using Permissions.Infrastructure.Persistence;

namespace Permissions.Infrastructure.Permissions;

internal sealed class PermissionChecker(PermissionsDbContext db) : IPermissionChecker
{
    public Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken = default)
        => db.PermissionGrants
            .AsNoTracking()
            .AnyAsync(
                g => g.UserId == userId && g.PermissionCode == permissionCode,
                cancellationToken);
}
