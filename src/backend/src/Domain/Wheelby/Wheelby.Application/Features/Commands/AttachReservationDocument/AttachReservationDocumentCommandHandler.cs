using AccessControl.Contracts;
using Documents.Contracts;
using MediatR;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Errors;
using Wheelby.Domain.Reservations;

namespace Wheelby.Application.Features.AttachReservationDocument;

// RF-NEG-10: un participante adjunta a la reserva un documento propio
// (acta de entrega o devolución, fotos del estado del vehículo...).
internal sealed class AttachReservationDocumentCommandHandler(
    IReservationRepository reservations,
    IVehicleRepository vehicles,
    IDocumentAccess documents,
    IWheelbyUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<AttachReservationDocumentCommand, Unit>
{
    public async Task<Unit> Handle(AttachReservationDocumentCommand command, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(command.ReservationId, cancellationToken)
            ?? throw new NotFoundException("La reserva no existe.", WheelbyErrorCodes.ReservationNotFound);

        var vehicle = await vehicles.GetByIdAsync(reservation.VehicleId, cancellationToken)
            ?? throw new NotFoundException("La reserva no existe.", WheelbyErrorCodes.ReservationNotFound);

        // Un tercero no distingue esta reserva de una inexistente.
        if (reservation.RenterId != currentUser.Id && vehicle.OwnerId != currentUser.Id)
            throw new NotFoundException("La reserva no existe.", WheelbyErrorCodes.ReservationNotFound);

        var document = await documents.GetInfoAsync(command.DocumentId, cancellationToken)
            ?? throw new NotFoundException("El documento no existe.", WheelbyErrorCodes.DocumentNotFound);

        if (document.OwnerId != currentUser.Id)
            throw new ForbiddenException("Solo puedes adjuntar documentos que subiste tú.", WheelbyErrorCodes.DocumentNotOwner);

        if (await reservations.DocumentAttachedAsync(reservation.Id, document.Id, cancellationToken))
            throw new ConflictException("El documento ya está adjunto a esta reserva.", WheelbyErrorCodes.ReservationDocumentDuplicate);

        if (await reservations.CountDocumentsAsync(reservation.Id, cancellationToken) >= ReservationDocument.MaxPerReservation)
            throw new ConflictException(
                $"Una reserva admite como máximo {ReservationDocument.MaxPerReservation} documentos.",
                WheelbyErrorCodes.ReservationDocumentLimit);

        var attached = ReservationDocument.Attach(reservation, document.Id, command.Kind, currentUser.Id, clock.UtcNow);
        await reservations.AddDocumentAsync(attached, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
