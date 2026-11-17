using MediatR;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Errors;

namespace Wheelby.Application.Features.GetVehicle;

// La ficha del vehículo la ve cualquier usuario con sesión.
internal sealed class GetVehicleQueryHandler(IVehicleReadStore vehicleReadStore)
    : IRequestHandler<GetVehicleQuery, VehicleSummaryDTO>
{
    public async Task<VehicleSummaryDTO> Handle(GetVehicleQuery query, CancellationToken cancellationToken)
        => await vehicleReadStore.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException("El vehículo no existe.", WheelbyErrorCodes.VehicleNotFound);
}
