using Permissions.Domain.PermissionRequests;

namespace Permissions.Application.Abstractions;

// Avisos de Gestión de permisos por el motor de notificaciones (RF-NOT-01 y 02).
// Se llama después de guardar y solo si la operación tuvo éxito. Es un efecto
// secundario: si falla, se registra en el log y nunca se propaga (no es outbox).
public interface IPermissionRequestNotifier
{
    // A los Administradores activos, excepto al solicitante.
    Task RequestCreatedAsync(PermissionRequest request, CancellationToken cancellationToken);
}
