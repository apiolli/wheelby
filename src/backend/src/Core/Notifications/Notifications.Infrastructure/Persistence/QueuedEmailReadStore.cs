using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Application.Common;
using Notifications.Application.DTOs;
using Notifications.Domain.Emails;
using Notifications.Domain.Emails.Enums;

namespace Notifications.Infrastructure.Persistence;

internal sealed class QueuedEmailReadStore(NotificationsDbContext db) : IQueuedEmailReadStore
{
    public async Task<PagedResult<QueuedEmailDTO>> ListAsync(
        EmailStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.QueuedEmails.AsNoTracking();
        if (status is not null)
            query = query.Where(e => e.Status == status);

        var total = await query.CountAsync(cancellationToken);

        var rows = await Project(query
                .OrderByDescending(e => e.CreatedAt).ThenBy(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<QueuedEmailDTO>(rows.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<QueuedEmailDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await Project(db.QueuedEmails.AsNoTracking().Where(e => e.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToDto(row);
    }

    // Se proyectan solo las columnas del DTO: el cuerpo (Body) nunca se lee.
    private static IQueryable<Row> Project(IQueryable<QueuedEmail> query)
        => query.Select(e => new Row(
            e.Id, e.To, e.Subject, e.Status, e.Attempts, e.CreatedAt,
            e.LastAttemptAt, e.SentAt, e.FailedAt, e.LastError));

    private static QueuedEmailDTO ToDto(Row r) => new(
        r.Id, r.To.Value, r.Subject, r.Status.ToString(), r.Attempts, r.CreatedAt,
        r.LastAttemptAt, r.SentAt, r.FailedAt, r.LastError);

    private sealed record Row(
        Guid Id,
        EmailAddress To,
        string Subject,
        EmailStatus Status,
        int Attempts,
        DateTime CreatedAt,
        DateTime? LastAttemptAt,
        DateTime? SentAt,
        DateTime? FailedAt,
        string? LastError);
}
