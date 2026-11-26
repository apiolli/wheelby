namespace Notifications.Contracts;

// Motor de notificaciones (RF-NOT-01 a 07): lo único que ven los otros módulos.
// Cada notificación llega por dos canales: la bandeja interna y un correo encolado,
// salvo que el destinatario haya desactivado el correo. Notificación y correo se
// guardan en una sola transacción. Llamarlo es un efecto secundario posterior al
// guardado de la operación: quien lo llama registra el fallo y no revierte (no es outbox).
public interface INotificationPublisher
{
    // Notifica a usuarios concretos (por Id). Un destinatario que no existe o está
    // deshabilitado se omite y se registra en el log.
    Task PublishAsync(
        NotificationMessage message,
        IReadOnlyCollection<Guid> recipientIds,
        CancellationToken cancellationToken = default);

    // Notifica a todos los Administradores activos, excepto los de excludeUserIds.
    Task PublishToAdministratorsAsync(
        NotificationMessage message,
        IReadOnlyCollection<Guid> excludeUserIds,
        CancellationToken cancellationToken = default);
}

// Title es también el asunto del correo (máximo 200 caracteres).
// ReferenceId es el Id de la solicitud o de la reserva que originó el aviso.
public sealed record NotificationMessage(string Type, string Title, string Message, Guid? ReferenceId);
