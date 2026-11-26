using AccessControl.Contracts;
using AccessControl.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistence;

internal sealed class UserDirectory(AccessControlDbContext db) : IUserDirectory, IAdministratorDirectory
{
    public async Task<UserContact?> GetContactAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.DisabledAt == null)
            .Select(u => new { u.Id, u.FullName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : new UserContact(row.Id, row.FullName, row.Email.Value);
    }

    // RF-NOT-01: rol Administrator, cuenta activada (ActivatedAt) y no deshabilitada (DisabledAt).
    public async Task<IReadOnlyList<UserContact>> ListActiveAdministratorsAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Users
            .AsNoTracking()
            .Where(u => u.Role == Role.Administrator && u.ActivatedAt != null && u.DisabledAt == null)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new { u.Id, u.FullName, u.Email })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new UserContact(r.Id, r.FullName, r.Email.Value)).ToList();
    }
}
