using AccessControl.Application.Abstractions;
using AccessControl.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistence;

internal sealed class UserReadStore(AccessControlDbContext db) : IUserReadStore
{
    public async Task<UserProfileDTO?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Proyección directa: no se carga el agregado ni se tocan hashes ni tokens.
        var row = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.FullName, u.Email, u.Role })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new UserProfileDTO(row.Id, row.FullName, row.Email.Value, RoleMapping.ToContract(row.Role));
    }
}