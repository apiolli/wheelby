using AccessControl.Contracts;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Errors;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Common;

// Operaciones que solo hace el dueño del vehículo (editar, activar, fotos).
internal static class VehicleOwnership
{
    public static async Task<Vehicle> GetOwnedAsync(
        IVehicleRepository vehicles,
        ICurrentUser currentUser,
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var vehicle = await vehicles.GetByIdAsync(vehicleId, cancellationToken)
            ?? throw new NotFoundException("El vehículo no existe.", WheelbyErrorCodes.VehicleNotFound);

        if (vehicle.OwnerId != currentUser.Id)
            throw new ForbiddenException("Solo el dueño puede modificar este vehículo.", WheelbyErrorCodes.VehicleNotOwner);

        return vehicle;
    }
}
