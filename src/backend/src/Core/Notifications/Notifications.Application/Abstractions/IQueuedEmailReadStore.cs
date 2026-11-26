using Notifications.Application.Common;
using Notifications.Application.DTOs;
using Notifications.Domain.Emails.Enums;

namespace Notifications.Application.Abstractions;

// Lecturas de la cola para el Administrador, del más nuevo al más viejo. Nunca el cuerpo.
public interface IQueuedEmailReadStore
{
    Task<PagedResult<QueuedEmailDTO>> ListAsync(
        EmailStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    Task<QueuedEmailDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
