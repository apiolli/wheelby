using Microsoft.EntityFrameworkCore;
using Permissions.Application.Abstractions;

namespace Permissions.Infrastructure.Persistence;

// RF-REP-03 (CA-REP-03.2): GroupBy + Count traducidos a SQL por EF. A memoria solo
// llegan los grupos (una fila por mes y estado), nunca las solicitudes.
internal sealed class PermissionRequestReportStore(PermissionsDbContext db) : IPermissionRequestReportStore
{
    public async Task<IReadOnlyList<PermissionRequestMonthStatusCount>> CountByMonthAndStatusAsync(
        DateTime startUtc, DateTime endUtc, Guid? requesterId, CancellationToken cancellationToken)
    {
        var query = db.PermissionRequests
            .AsNoTracking()
            .Where(r => r.CreatedAt >= startUtc && r.CreatedAt < endUtc);

        if (requesterId is not null)
            query = query.Where(r => r.RequesterId == requesterId);

        var groups = await query
            .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month, r.Status })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Status, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return groups
            .Select(g => new PermissionRequestMonthStatusCount(g.Year, g.Month, g.Status.ToString(), g.Count))
            .ToList();
    }
}
