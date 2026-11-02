using Permissions.Contracts;

namespace Wheelby.Test.TestSupport;

// Doble de IPermissionChecker SOLO para pruebas unitarias de F1 (nunca en la prueba de integración).
public sealed class FakePermissionChecker : IPermissionChecker
{
    private readonly HashSet<(Guid userId, string code)> _grants = [];

    public void Grant(Guid userId, string code) => _grants.Add((userId, code));

    public Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_grants.Contains((userId, permissionCode)));
}
