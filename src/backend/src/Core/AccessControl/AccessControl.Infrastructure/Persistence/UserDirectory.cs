using AccessControl.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistence;

internal sealed class UserDirectory(AccessControlDbContext db) : IUserDirectory
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
}
