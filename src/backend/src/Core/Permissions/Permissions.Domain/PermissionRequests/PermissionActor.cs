namespace Permissions.Domain.PermissionRequests;

// Tipo de actor que ejecuta cada transición.
// La máquina verifica el tipo de actor; que el usuario concreto sea
// Administrador lo exige el endpoint (grupo de rutas de administración).
public enum PermissionActor
{
    Administrator = 0,
    System = 1
}
