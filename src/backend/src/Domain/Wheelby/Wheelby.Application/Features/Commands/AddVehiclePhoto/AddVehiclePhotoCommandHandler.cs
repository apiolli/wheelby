using AccessControl.Contracts;
using Documents.Contracts;
using MediatR;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Domain.Errors;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Features.AddVehiclePhoto;

internal sealed class AddVehiclePhotoCommandHandler(
    IVehicleRepository vehicles,
    IDocumentAccess documents,
    IWheelbyUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<AddVehiclePhotoCommand, Unit>
{
    public async Task<Unit> Handle(AddVehiclePhotoCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await VehicleOwnership.GetOwnedAsync(vehicles, currentUser, command.VehicleId, cancellationToken);

        var document = await documents.GetInfoAsync(command.DocumentId, cancellationToken)
            ?? throw new NotFoundException("El documento no existe.", WheelbyErrorCodes.DocumentNotFound);

        if (document.OwnerId != currentUser.Id)
            throw new ForbiddenException("Solo puedes usar imágenes que subiste tú.", WheelbyErrorCodes.DocumentNotOwner);

        // La foto se muestra a cualquier usuario con sesión: solo se aceptan imágenes.
        if (!document.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("La foto debe ser una imagen (JPEG, PNG o WebP).", WheelbyErrorCodes.VehiclePhotoInvalid);

        if (await vehicles.GetPhotoAsync(vehicle.Id, document.Id, cancellationToken) is not null)
            return Unit.Value; // Idempotente: ya estaba.

        if (await vehicles.CountPhotosAsync(vehicle.Id, cancellationToken) >= VehiclePhoto.MaxPerVehicle)
            throw new ConflictException(
                $"Un vehículo admite como máximo {VehiclePhoto.MaxPerVehicle} fotos.", WheelbyErrorCodes.VehiclePhotoLimit);

        await vehicles.AddPhotoAsync(new VehiclePhoto(vehicle.Id, document.Id, clock.UtcNow), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
