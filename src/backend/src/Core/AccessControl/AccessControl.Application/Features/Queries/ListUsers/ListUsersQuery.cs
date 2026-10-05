using AccessControl.Application.Common;
using AccessControl.Application.DTOs;
using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.ListUsers;

public sealed record ListUsersQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<UserSummaryDTO>>;
