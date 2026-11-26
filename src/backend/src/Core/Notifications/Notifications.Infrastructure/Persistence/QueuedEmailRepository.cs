using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Notifications.Application.Abstractions;
using Notifications.Domain.Emails;
using Notifications.Domain.Emails.Enums;

namespace Notifications.Infrastructure.Persistence;

internal sealed class QueuedEmailRepository(NotificationsDbContext db) : IQueuedEmailRepository
{
    public async Task AddAsync(QueuedEmail email, CancellationToken cancellationToken)
        => await db.QueuedEmails.AddAsync(email, cancellationToken);

    public async Task<IQueueTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        => new EfQueueTransaction(await db.Database.BeginTransactionAsync(cancellationToken));

    // RF-NOT-12 (CA-NOT-12.2): FOR UPDATE SKIP LOCKED bloquea la fila tomada hasta el
    // commit, y otro procesador que corra a la vez la salta en lugar de esperarla.
    // Se lee con ToListAsync sin componer: EF no envuelve el SQL en una subconsulta.
    public async Task<QueuedEmail?> ClaimNextPendingAsync(
        IReadOnlyCollection<Guid> excludeIds, CancellationToken cancellationToken)
    {
        var pending = nameof(EmailStatus.Pending);
        var exclude = excludeIds.ToArray();

        var rows = await db.QueuedEmails
            .FromSql($"""
                SELECT * FROM notifications."QueuedEmails"
                WHERE "Status" = {pending} AND NOT ("Id" = ANY({exclude}))
                ORDER BY "CreatedAt", "Id"
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        return rows.FirstOrDefault();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);

    private sealed class EfQueueTransaction(IDbContextTransaction transaction) : IQueueTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken)
            => transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
