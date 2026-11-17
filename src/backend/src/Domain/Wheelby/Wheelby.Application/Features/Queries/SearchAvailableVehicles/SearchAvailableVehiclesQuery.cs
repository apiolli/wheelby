using Shared.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Features.SearchAvailableVehicles;

public sealed record SearchAvailableVehiclesQuery(
    DateTime From,
    DateTime To,
    int Page = 1,
    int PageSize = 20,
    string? Currency = null,
    string? City = null,
    VehicleType? Type = null) : IQuery<PagedResult<VehicleSummaryDTO>>;
