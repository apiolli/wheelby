using AccessControl.Contracts;
using MediatR;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.SearchAvailableVehicles;

internal sealed class SearchAvailableVehiclesQueryHandler(
    IVehicleReadStore vehicleReadStore,
    ICurrentUser currentUser)
    : IRequestHandler<SearchAvailableVehiclesQuery, PagedResult<VehicleSummaryDTO>>
{
    public Task<PagedResult<VehicleSummaryDTO>> Handle(
        SearchAvailableVehiclesQuery query,
        CancellationToken cancellationToken)
        => vehicleReadStore.SearchAvailableAsync(
            query.From,
            query.To,
            currentUser.Id,
            query.Page,
            query.PageSize,
            cancellationToken);
}
