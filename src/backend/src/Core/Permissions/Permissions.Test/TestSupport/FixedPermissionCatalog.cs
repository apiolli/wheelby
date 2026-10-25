using Permissions.Application.Abstractions;

namespace Permissions.Test.Support;

// Catálogo fijo con los códigos que la prueba decide.
public sealed class FixedPermissionCatalog(params string[] codes) : IPermissionCatalog
{
    public bool Exists(string permissionCode) => codes.Contains(permissionCode);
}
