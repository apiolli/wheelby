using Notifications.Application.Abstractions;
using Notifications.Domain.Emails;
using Notifications.Domain.Emails.Enums;

namespace Notifications.Test.TestSupport;

// Doble en memoria de la cola: lista propia de cada prueba. Igual que el repositorio
// real, solo entrega correos Pending, del más viejo al más nuevo.
public sealed class FakeQueuedEmailRepository(params QueuedEmail[] seed) : IQueuedEmailRepository
{
    public List<QueuedEmail> Emails { get; } = [.. seed];
    public int SaveCount { get; private set; }

    public Task AddAsync(QueuedEmail email, CancellationToken cancellationToken)
    {
        Emails.Add(email);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<QueuedEmail>> GetPendingAsync(int maxCount, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<QueuedEmail>>(Emails
            .Where(e => e.Status == EmailStatus.Pending)
            .OrderBy(e => e.CreatedAt)
            .Take(maxCount)
            .ToList());

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
