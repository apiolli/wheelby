using AccessControl.Contracts;
using MediatR;
using Permissions.Application.Abstractions;
using Permissions.Application.Common;
using Permissions.Application.DTOs;

namespace Permissions.Application.Features.ListMyRequests;

internal sealed class ListMyRequestsQueryHandler(
    IPermissionRequestReadStore reader,
    ICurrentUser currentUser)
    : IRequestHandler<ListMyRequestsQuery, PagedResult<PermissionRequestSummaryDTO>>
{
    public Task<PagedResult<PermissionRequestSummaryDTO>> Handle(
        ListMyRequestsQuery request,
        CancellationToken cancellationToken)
        => reader.ListByRequesterAsync(currentUser.Id, request.Page, request.PageSize, cancellationToken);
}
