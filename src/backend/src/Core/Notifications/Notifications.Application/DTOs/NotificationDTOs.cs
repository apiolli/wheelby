namespace Notifications.Application.DTOs;

// RF-NOT-04: un elemento de la bandeja. emailSent indica si se encoló su correo.
public sealed record NotificationDTO(
    Guid Id,
    Guid RecipientId,
    string Type,
    string Title,
    string Message,
    Guid? ReferenceId,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt,
    bool EmailSent);

// Página de la bandeja con el total de no leídas del usuario (sin importar el filtro).
public sealed record NotificationPageDTO(
    IReadOnlyList<NotificationDTO> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int UnreadCount);
