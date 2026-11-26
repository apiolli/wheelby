using AccessControl.Contracts;
using Microsoft.Extensions.Logging;
using Notifications.Contracts;
using Permissions.Application.Abstractions;
using Permissions.Domain.PermissionRequests;

namespace Permissions.Infrastructure.Notifiers;

// Traduce los eventos de Gestión de permisos a notificaciones (RF-NOT-01 y 02).
// Permissions solo conoce Notifications.Contracts y AccessControl.Contracts (RD-03).
// Si el host no registró el motor (Permissions usado solo), avisa en el log y no notifica.
internal sealed class PermissionRequestNotifier(
    INotificationPublisher? publisher,
    IUserDirectory? users,
    ILogger<PermissionRequestNotifier> logger) : IPermissionRequestNotifier
{
    public Task RequestCreatedAsync(PermissionRequest request, CancellationToken cancellationToken)
        => SafelyAsync(request, async p =>
        {
            var requester = users is null ? null : await users.GetContactAsync(request.RequesterId, cancellationToken);
            var requesterName = requester?.FullName ?? "Un usuario";

            var message = new NotificationMessage(
                NotificationTypes.PermissionRequestCreated,
                "Nueva solicitud de permiso",
                $"{requesterName} solicitó el permiso {request.PermissionCode}. " +
                "Revísala en Gestión de permisos para aprobarla o rechazarla.",
                request.Id);

            // CA-NOT-01.3: un Administrador que solicita no se notifica a sí mismo.
            await p.PublishToAdministratorsAsync(message, [request.RequesterId], cancellationToken);
        });

    public Task RequestApprovedAsync(PermissionRequest request, CancellationToken cancellationToken)
        => SafelyAsync(request, p => p.PublishAsync(
            new NotificationMessage(
                NotificationTypes.PermissionRequestApproved,
                "Tu solicitud de permiso fue aprobada",
                $"Tu solicitud del permiso {request.PermissionCode} fue aprobada. Ya puedes usarlo.",
                request.Id),
            [request.RequesterId],
            cancellationToken));

    public Task RequestRejectedAsync(PermissionRequest request, CancellationToken cancellationToken)
        => SafelyAsync(request, p => p.PublishAsync(
            new NotificationMessage(
                NotificationTypes.PermissionRequestRejected,
                "Tu solicitud de permiso fue rechazada",
                $"Tu solicitud del permiso {request.PermissionCode} fue rechazada.\n\nMotivo: {request.RejectionReason}",
                request.Id),
            [request.RequesterId],
            cancellationToken));

    // Efecto secundario: un fallo se registra y nunca llega a la operación del usuario.
    private async Task SafelyAsync(PermissionRequest request, Func<INotificationPublisher, Task> notify)
    {
        if (publisher is null)
        {
            logger.LogWarning("Notifications module is not registered; permission request {RequestId} was not notified.",
                request.Id);
            return;
        }

        try
        {
            await notify(publisher);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not notify about permission request {RequestId} ({Status}).",
                request.Id, request.Status);
        }
    }
}
