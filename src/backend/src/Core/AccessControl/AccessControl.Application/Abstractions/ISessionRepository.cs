using AccessControl.Domain.Sessions;

namespace AccessControl.Application.Abstractions;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Session session, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}