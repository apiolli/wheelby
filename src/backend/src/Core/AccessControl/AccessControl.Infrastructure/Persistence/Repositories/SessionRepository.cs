using AccessControl.Application.Abstractions;
using AccessControl.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistence;

internal sealed class SessionRepository(AccessControlDbContext db) : ISessionRepository
{
    public Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.Sessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task AddAsync(Session session, CancellationToken cancellationToken)
        => await db.Sessions.AddAsync(session, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<Session>> GetActiveByUserIdAsync(
        Guid userId, DateTime utcNow, CancellationToken cancellationToken)
        => await db.Sessions
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);    
}