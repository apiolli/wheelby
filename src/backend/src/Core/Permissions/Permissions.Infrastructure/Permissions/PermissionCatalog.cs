using Permissions.Application.Abstractions;
using Permissions.Contracts;

namespace Permissions.Infrastructure.Permissions;

// Catálogo construido con los permisos registrados en DI (IEnumerable<IPermissionDefinition>).
internal sealed class PermissionCatalog(IEnumerable<IPermissionDefinition> definitions) : IPermissionCatalog
{
    private readonly HashSet<string> _codes = definitions
        .Select(definition => definition.Code)
        .ToHashSet(StringComparer.Ordinal);

    public bool Exists(string permissionCode) => _codes.Contains(permissionCode);
}
