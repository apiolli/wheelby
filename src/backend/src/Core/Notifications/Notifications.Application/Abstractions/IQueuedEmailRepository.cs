using Notifications.Domain.Emails;

namespace Notifications.Application.Abstractions;

public interface IQueuedEmailRepository
{
    Task AddAsync(QueuedEmail email, CancellationToken cancellationToken);

    // RF-NOT-12: cada correo se procesa dentro de su propia transacción.
    Task<IQueueTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    // Toma el Pending más viejo que no esté en excludeIds y lo bloquea hasta el fin de la
    // transacción (FOR UPDATE SKIP LOCKED): otro procesador simultáneo lo salta.
    // null si no queda ninguno libre.
    Task<QueuedEmail?> ClaimNextPendingAsync(IReadOnlyCollection<Guid> excludeIds, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IQueueTransaction : IAsyncDisposable
{
    // Sin CommitAsync, al liberarse se revierte.
    Task CommitAsync(CancellationToken cancellationToken);
}
