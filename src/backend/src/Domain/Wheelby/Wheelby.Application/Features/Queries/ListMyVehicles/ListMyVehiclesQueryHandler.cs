using AccessControl.Contracts;
using MediatR;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.ListMyVehicles;

// Vehículos publicados por el anfitrión autenticado.
internal sealed class ListMyVehiclesQueryHandler(
    IVehicleReadStore vehicleReadStore,
    ICurrentUser currentUser)
    : IRequestHandler<ListMyVehiclesQuery, PagedResult<VehicleSummaryDTO>>
{
    public Task<PagedResult<VehicleSummaryDTO>> Handle(
        ListMyVehiclesQuery query,
        CancellationToken cancellationToken)
        => vehicleReadStore.ListByOwnerAsync(currentUser.Id, query.Page, query.PageSize, cancellationToken);
}
