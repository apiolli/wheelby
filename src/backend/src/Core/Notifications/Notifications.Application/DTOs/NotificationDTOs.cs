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
// Trae los mismos campos de paginación que PagedResult<T>, para que el cliente pagine
// la bandeja igual que cualquier otra lista.
public sealed record NotificationPageDTO(
    IReadOnlyList<NotificationDTO> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int UnreadCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
