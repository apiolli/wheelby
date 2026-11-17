using Shared.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.ListMyVehicles;

public sealed record ListMyVehiclesQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<VehicleSummaryDTO>>;
