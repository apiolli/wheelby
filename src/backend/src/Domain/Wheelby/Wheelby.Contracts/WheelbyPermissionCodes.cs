namespace Wheelby.Contracts;

// Códigos de permiso conocidos por el módulo Wheelby.
// Cada módulo registra sus permisos en DI como IPermissionDefinition.
// El Core no conoce Wheelby; esta inversión de dependencia lo mantiene independiente.
public static class WheelbyPermissionCodes
{
    public const string PublishVehicle = "wheelby.publish-vehicle";
}
