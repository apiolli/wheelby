using AccessControl.Contracts;
using MediatR;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Domain.Errors;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Features.UpdateVehicle;

// Solo el dueño edita. El precio nuevo no toca reservas existentes (total congelado).
internal sealed class UpdateVehicleCommandHandler(
    IVehicleRepository vehicles,
    IWheelbyUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<UpdateVehicleCommand, Unit>
{
    public async Task<Unit> Handle(UpdateVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await VehicleOwnership.GetOwnedAsync(vehicles, currentUser, command.Id, cancellationToken);

        var plate = Vehicle.NormalizePlate(command.Plate);
        if (plate != vehicle.Plate && await vehicles.PlateExistsAsync(plate, vehicle.Id, cancellationToken))
            throw new ConflictException("Ya existe un vehículo con esa placa.", WheelbyErrorCodes.VehiclePlateTaken);

        vehicle.Update(
            command.Brand, command.Model, command.Year, command.Plate, command.PricePerDay,
            command.Type, command.City, command.Description, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
