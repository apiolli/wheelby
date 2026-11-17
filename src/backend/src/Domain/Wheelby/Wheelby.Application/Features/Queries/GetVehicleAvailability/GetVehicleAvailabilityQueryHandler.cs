using MediatR;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Errors;

namespace Wheelby.Application.Features.GetVehicleAvailability;

// Rangos bloqueados del vehículo para que el calendario los deshabilite
// antes de que el inquilino choque con un 409 al reservar.
internal sealed class GetVehicleAvailabilityQueryHandler(IVehicleReadStore vehicleReadStore)
    : IRequestHandler<GetVehicleAvailabilityQuery, VehicleAvailabilityDTO>
{
    public async Task<VehicleAvailabilityDTO> Handle(
        GetVehicleAvailabilityQuery query,
        CancellationToken cancellationToken)
    {
        _ = await vehicleReadStore.GetByIdAsync(query.VehicleId, cancellationToken)
            ?? throw new NotFoundException("El vehículo no existe.", WheelbyErrorCodes.VehicleNotFound);

        var blocked = await vehicleReadStore.ListBlockedRangesAsync(
            query.VehicleId, query.From, query.To, cancellationToken);

        return new VehicleAvailabilityDTO(query.VehicleId, query.From, query.To, blocked);
    }
}
