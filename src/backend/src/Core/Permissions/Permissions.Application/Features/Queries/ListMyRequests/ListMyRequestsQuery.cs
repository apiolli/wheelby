using Permissions.Application.Common;
using Permissions.Application.DTOs;
using Shared.Application.Abstractions;

namespace Permissions.Application.Features.ListMyRequests;

public sealed record ListMyRequestsQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<PermissionRequestSummaryDTO>>;
