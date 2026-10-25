using Microsoft.EntityFrameworkCore;
using Permissions.Application.Abstractions;
using Permissions.Application.Common;
using Permissions.Application.DTOs;
using Permissions.Domain.PermissionRequests;

namespace Permissions.Infrastructure.Persistence;

internal sealed class PermissionRequestReadStore(PermissionsDbContext db) : IPermissionRequestReadStore
{
    public Task<PagedResult<PermissionRequestSummaryDTO>> ListPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.PermissionRequests
            .AsNoTracking()
            .Where(r => r.Status == PermissionRequestStatus.Pending);

        return ToPageAsync(query, page, pageSize, cancellationToken);
    }

    public Task<PagedResult<PermissionRequestSummaryDTO>> ListByRequesterAsync(
        Guid requesterId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.PermissionRequests
            .AsNoTracking()
            .Where(r => r.RequesterId != requesterId);

        return ToPageAsync(query, page, pageSize, cancellationToken);
    }

    private static async Task<PagedResult<PermissionRequestSummaryDTO>> ToPageAsync(
        IQueryable<PermissionRequest> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.RequesterId,
                r.PermissionCode,
                r.Status,
                r.ApproverId,
                r.CreatedAt,
                r.ResolvedAt,
                r.RejectionReason
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new PermissionRequestSummaryDTO(
                r.Id,
                r.RequesterId,
                r.PermissionCode,
                r.Status.ToString(),
                r.ApproverId,
                r.CreatedAt,
                r.ResolvedAt,
                r.RejectionReason))
            .ToList();

        return new PagedResult<PermissionRequestSummaryDTO>(items, page, pageSize, total);
    }
}
