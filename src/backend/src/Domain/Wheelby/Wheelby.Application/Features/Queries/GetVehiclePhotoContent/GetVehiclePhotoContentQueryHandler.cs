using Documents.Contracts;
using MediatR;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Errors;

namespace Wheelby.Application.Features.GetVehiclePhotoContent;

// Document.CanBeAccessedBy solo deja ver al dueño; un inquilino no vería las fotos
// por /documents. Aquí cualquier usuario con sesión ve una foto, pero solo si
// está vinculada a ese vehículo: no sirve para leer documentos arbitrarios.
internal sealed class GetVehiclePhotoContentQueryHandler(
    IVehicleRepository vehicles,
    IDocumentAccess documents)
    : IRequestHandler<GetVehiclePhotoContentQuery, FileContentDTO>
{
    public async Task<FileContentDTO> Handle(GetVehiclePhotoContentQuery query, CancellationToken cancellationToken)
    {
        _ = await vehicles.GetPhotoAsync(query.VehicleId, query.DocumentId, cancellationToken)
            ?? throw new NotFoundException("La foto no existe.", WheelbyErrorCodes.VehiclePhotoNotFound);

        var file = await documents.OpenReadAsync(query.DocumentId, cancellationToken)
            ?? throw new NotFoundException("La foto ya no está disponible.", WheelbyErrorCodes.VehiclePhotoNotFound);

        return new FileContentDTO(file.Content, file.ContentType, file.FileName);
    }
}
