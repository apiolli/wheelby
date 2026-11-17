using AccessControl.Contracts;
using Documents.Contracts;
using MediatR;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Errors;

namespace Wheelby.Application.Features.GetReservationDocumentContent;

// Document.CanBeAccessedBy solo deja ver al dueño del archivo; aquí la regla es
// de Wheelby: cualquier participante de la reserva (o el Administrador) lo ve,
// siempre que el documento esté adjunto a esa reserva.
internal sealed class GetReservationDocumentContentQueryHandler(
    IReservationReadStore reservations,
    IReservationRepository reservationRepository,
    IDocumentAccess documents,
    ICurrentUser currentUser)
    : IRequestHandler<GetReservationDocumentContentQuery, FileContentDTO>
{
    public async Task<FileContentDTO> Handle(GetReservationDocumentContentQuery query, CancellationToken cancellationToken)
    {
        await ReservationAccess.GetVisibleAsync(reservations, currentUser, query.ReservationId, cancellationToken);

        if (!await reservationRepository.DocumentAttachedAsync(query.ReservationId, query.DocumentId, cancellationToken))
            throw new NotFoundException("El documento no está adjunto a esta reserva.", WheelbyErrorCodes.DocumentNotFound);

        var file = await documents.OpenReadAsync(query.DocumentId, cancellationToken)
            ?? throw new NotFoundException("El documento ya no está disponible.", WheelbyErrorCodes.DocumentNotFound);

        return new FileContentDTO(file.Content, file.ContentType, file.FileName);
    }
}
