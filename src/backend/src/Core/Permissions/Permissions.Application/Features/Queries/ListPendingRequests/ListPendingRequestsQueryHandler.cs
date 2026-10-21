using MediatR;
using Permissions.Application.Abstractions;
using Permissions.Application.Common;
using Permissions.Application.DTOs;

namespace Permissions.Application.Features.ListPendingRequests;

internal sealed class ListPendingRequestsQueryHandler(IPermissionRequestReadStore reader)
    : IRequestHandler<ListPendingRequestsQuery, PagedResult<PermissionRequestSummaryDTO>>
{
    public Task<PagedResult<PermissionRequestSummaryDTO>> Handle(
        ListPendingRequestsQuery request,
        CancellationToken cancellationToken)
        => reader.ListPendingAsync(request.Page, request.PageSize, cancellationToken);
}
