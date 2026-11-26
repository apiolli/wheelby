using Notifications.Application.Abstractions;
using Notifications.Domain.Emails;
using Notifications.Domain.Emails.Enums;

namespace Notifications.Test.TestSupport;

// Doble en memoria de la cola: lista propia de cada prueba. Igual que el repositorio
// real, solo entrega correos Pending, del más viejo al más nuevo, y salta los excluidos.
// El bloqueo de filas (FOR UPDATE SKIP LOCKED) se verifica a mano contra PostgreSQL.
public sealed class FakeQueuedEmailRepository(params QueuedEmail[] seed) : IQueuedEmailRepository
{
    public List<QueuedEmail> Emails { get; } = [.. seed];
    public int SaveCount { get; private set; }
    public int CommitCount { get; private set; }

    public Task AddAsync(QueuedEmail email, CancellationToken cancellationToken)
    {
        Emails.Add(email);
        return Task.CompletedTask;
    }

    public Task<IQueueTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        => Task.FromResult<IQueueTransaction>(new FakeTransaction(this));

    public Task<QueuedEmail?> ClaimNextPendingAsync(
        IReadOnlyCollection<Guid> excludeIds, CancellationToken cancellationToken)
        => Task.FromResult(Emails
            .Where(e => e.Status == EmailStatus.Pending && !excludeIds.Contains(e.Id))
            .OrderBy(e => e.CreatedAt)
            .FirstOrDefault());

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    private sealed class FakeTransaction(FakeQueuedEmailRepository owner) : IQueueTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken)
        {
            owner.CommitCount++;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
