using Shared.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.SearchAvailableVehicles;

public sealed record SearchAvailableVehiclesQuery(
    DateTime From,
    DateTime To,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<VehicleSummaryDTO>>;
