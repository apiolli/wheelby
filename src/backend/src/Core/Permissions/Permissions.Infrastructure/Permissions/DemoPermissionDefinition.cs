using Permissions.Contracts;

namespace Permissions.Infrastructure.Permissions;

// Permiso que el propio módulo Permissions registra en el catálogo.
internal sealed class DemoPermissionDefinition : IPermissionDefinition
{
    public string Code => PermissionCodes.DemoAction;

    public string Description => "Acción de demostración del Core.";
}
