namespace Permissions.Application.Abstractions;

// Catálogo de permisos registrados por los módulos en DI.
public interface IPermissionCatalog
{
    bool Exists(string permissionCode);
}
