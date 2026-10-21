using Microsoft.EntityFrameworkCore;
using Permissions.Application.Abstractions;
using Permissions.Domain.PermissionGrants;

namespace Permissions.Infrastructure.Persistence;

internal sealed class PermissionGrantRepository(PermissionsDbContext db) : IPermissionGrantRepository
{
    public Task<bool> ExistsAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken)
        => db.PermissionGrants.AnyAsync(
            g => g.UserId == userId && g.PermissionCode == permissionCode,
            cancellationToken);

    public async Task AddAsync(PermissionGrant grant, CancellationToken cancellationToken)
        => await db.PermissionGrants.AddAsync(grant, cancellationToken);
}
