using AccessControl.Contracts;
using MediatR;
using Permissions.Contracts;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Vehicles;
using Wheelby.Contracts;

namespace Wheelby.Application.Features.PublishVehicle;

internal sealed class PublishVehicleCommandHandler(
    IVehicleRepository vehicles,
    IWheelbyUnitOfWork unitOfWork,
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<PublishVehicleCommand, Guid>
{
    public async Task<Guid> Handle(
        PublishVehicleCommand command,
        CancellationToken cancellationToken)
    {
        if (await vehicles.PlateExistsAsync(command.Plate, cancellationToken))
            throw new ConflictException("Ya existe un vehículo con esa placa.");

        var vehicle = Vehicle.Create(
            currentUser.Id,
            command.Brand,
            command.Model,
            command.Year,
            command.Plate,
            command.PricePerDay,
            clock.UtcNow);

        await vehicles.AddAsync(vehicle, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return vehicle.Id;
    }
}
