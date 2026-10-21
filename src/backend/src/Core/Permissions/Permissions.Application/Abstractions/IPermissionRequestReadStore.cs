using Permissions.Application.Common;
using Permissions.Application.DTOs;

namespace Permissions.Application.Abstractions;

public interface IPermissionRequestReadStore
{
    Task<PagedResult<PermissionRequestSummaryDTO>> ListPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<PermissionRequestSummaryDTO>> ListByRequesterAsync(
        Guid requesterId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
