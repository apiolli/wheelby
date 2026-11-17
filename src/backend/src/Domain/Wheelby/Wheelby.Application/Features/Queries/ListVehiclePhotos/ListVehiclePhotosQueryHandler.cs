using MediatR;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Errors;

namespace Wheelby.Application.Features.ListVehiclePhotos;

internal sealed class ListVehiclePhotosQueryHandler(IVehicleReadStore vehicles)
    : IRequestHandler<ListVehiclePhotosQuery, IReadOnlyList<VehiclePhotoDTO>>
{
    public async Task<IReadOnlyList<VehiclePhotoDTO>> Handle(ListVehiclePhotosQuery query, CancellationToken cancellationToken)
    {
        _ = await vehicles.GetByIdAsync(query.VehicleId, cancellationToken)
            ?? throw new NotFoundException("El vehículo no existe.", WheelbyErrorCodes.VehicleNotFound);

        return await vehicles.ListPhotosAsync(query.VehicleId, cancellationToken);
    }
}
