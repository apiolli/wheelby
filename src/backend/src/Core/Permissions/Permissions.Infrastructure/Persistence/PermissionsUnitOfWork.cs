using Permissions.Application.Abstractions;

namespace Permissions.Infrastructure.Persistence;

internal sealed class PermissionsUnitOfWork(PermissionsDbContext db) : IPermissionsUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
