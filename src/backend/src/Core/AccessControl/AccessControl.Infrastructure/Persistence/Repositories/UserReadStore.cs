using AccessControl.Application.Abstractions;
using AccessControl.Application.Common;
using AccessControl.Application.DTOs;
using AccessControl.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistence;

internal sealed class UserReadStore(AccessControlDbContext db) : IUserReadStore
{
    public async Task<UserProfileDTO?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.FullName, u.Email, u.Role })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new UserProfileDTO(row.Id, row.FullName, row.Email.Value, RoleMapping.ToContract(row.Role));
    }

    public async Task<PagedResult<UserSummaryDTO>> ListAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.Id, u.FullName, u.Email, u.Role, u.CreatedAt, u.ActivatedAt, u.DisabledAt })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new UserSummaryDTO(
                r.Id, r.FullName, r.Email.Value, RoleMapping.ToContract(r.Role),
                r.DisabledAt is not null ? UserStatus.Disabled
                    : r.ActivatedAt is not null ? UserStatus.Active
                    : UserStatus.PendingActivation,
                r.CreatedAt))
            .ToList();

        return new PagedResult<UserSummaryDTO>(items, page, pageSize, total);
    }
}