using Permissions.Application.Common;
using Permissions.Application.DTOs;
using Shared.Application.Abstractions;

namespace Permissions.Application.Features.ListPendingRequests;

public sealed record ListPendingRequestsQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<PermissionRequestSummaryDTO>>;
