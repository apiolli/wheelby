namespace Permissions.Contracts;

// Permite a otros módulos comprobar si un usuario tiene un permiso,
// sin conocer nada del esquema de la base de datos.
public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken = default);
}
