using AccessControl.Application.Abstractions;
using AccessControl.Application.Common;
using AccessControl.Application.DTOs;
using MediatR;

namespace AccessControl.Application.Features.ListUsers;

internal sealed class ListUsersQueryHandler(IUserReadStore reader)
    : IRequestHandler<ListUsersQuery, PagedResult<UserSummaryDTO>>
{
    public Task<PagedResult<UserSummaryDTO>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
        => reader.ListAsync(request.Page, request.PageSize, cancellationToken);
}