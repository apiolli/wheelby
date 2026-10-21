using Microsoft.EntityFrameworkCore;
using Permissions.Application.Abstractions;
using Permissions.Domain.PermissionRequests;

namespace Permissions.Infrastructure.Persistence;

internal sealed class PermissionRequestRepository(PermissionsDbContext db) : IPermissionRequestRepository
{
    public Task<PermissionRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.PermissionRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> ExistsPendingAsync(
        Guid requesterId,
        string permissionCode,
        CancellationToken cancellationToken)
        => db.PermissionRequests.AnyAsync(
            r => r.RequesterId == requesterId
                 && r.PermissionCode == permissionCode
                 && r.Status == PermissionRequestStatus.Pending,
            cancellationToken);

    public async Task AddAsync(PermissionRequest request, CancellationToken cancellationToken)
        => await db.PermissionRequests.AddAsync(request, cancellationToken);
}
