using AccessControl.Contracts;
using MediatR;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Domain.Errors;

namespace Wheelby.Application.Features.RemoveVehiclePhoto;

internal sealed class RemoveVehiclePhotoCommandHandler(
    IVehicleRepository vehicles,
    IWheelbyUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<RemoveVehiclePhotoCommand, Unit>
{
    public async Task<Unit> Handle(RemoveVehiclePhotoCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await VehicleOwnership.GetOwnedAsync(vehicles, currentUser, command.VehicleId, cancellationToken);

        var photo = await vehicles.GetPhotoAsync(vehicle.Id, command.DocumentId, cancellationToken)
            ?? throw new NotFoundException("La foto no existe.", WheelbyErrorCodes.VehiclePhotoNotFound);

        vehicles.RemovePhoto(photo);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
