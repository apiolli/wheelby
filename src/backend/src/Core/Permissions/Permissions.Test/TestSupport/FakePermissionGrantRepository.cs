using Permissions.Application.Abstractions;
using Permissions.Domain.PermissionGrants;

namespace Permissions.Test.Support;

// Doble en memoria del repositorio de concesiones: lista propia de cada prueba.
public sealed class FakePermissionGrantRepository(params PermissionGrant[] seed)
    : IPermissionGrantRepository
{
    public List<PermissionGrant> Grants { get; } = [.. seed];

    public Task<bool> ExistsAsync(Guid userId, string permissionCode, CancellationToken cancellationToken)
        => Task.FromResult(Grants.Any(g => g.UserId == userId && g.PermissionCode == permissionCode));

    public Task AddAsync(PermissionGrant grant, CancellationToken cancellationToken)
    {
        Grants.Add(grant);
        return Task.CompletedTask;
    }
}
