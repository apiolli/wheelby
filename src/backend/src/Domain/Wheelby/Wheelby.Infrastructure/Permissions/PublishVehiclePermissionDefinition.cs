using AccessControl.Contracts;
using Permissions.Contracts;
using Wheelby.Contracts;

namespace Wheelby.Infrastructure.Permissions;

// Permiso que el módulo Wheelby registra en el catálogo de permisos.
internal sealed class PublishVehiclePermissionDefinition : IPermissionDefinition
{
    public string Code => WheelbyPermissionCodes.PublishVehicle;
    public string Description => "Permite publicar vehículos en el marketplace de Wheelby.";
}
