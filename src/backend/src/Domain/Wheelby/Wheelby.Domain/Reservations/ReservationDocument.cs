using SharedKernel.Domain;
using Wheelby.Domain.Errors;
using SharedKernel.Exceptions;

namespace Wheelby.Domain.Reservations;

// Documento adjunto a una reserva (RF-NEG-10). Reemplaza el módulo de
// inspecciones con una sola tabla. RD-03: Wheelby guarda solo el DocumentId;
// Documents no sabe que el archivo pertenece a una reserva.
public sealed class ReservationDocument : Entity<Guid>
{
    public const int MaxPerReservation = 20;

    // Estados en los que se puede adjuntar: desde que se confirma hasta que termina.
    public static readonly IReadOnlySet<ReservationStatus> AttachableStates =
        new HashSet<ReservationStatus>
        {
            ReservationStatus.Confirmed,
            ReservationStatus.InProgress,
            ReservationStatus.Completed
        };

    public Guid ReservationId { get; private set; }
    public Guid DocumentId { get; private set; }
    public ReservationDocumentKind Kind { get; private set; }
    public Guid AttachedBy { get; private set; }
    public DateTime AttachedAt { get; private set; }

    private ReservationDocument() { }

    private ReservationDocument(Guid reservationId, Guid documentId, ReservationDocumentKind kind, Guid attachedBy, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        ReservationId = reservationId;
        DocumentId = documentId;
        Kind = kind;
        AttachedBy = attachedBy;
        AttachedAt = utcNow;
    }

    public static ReservationDocument Attach(
        Reservation reservation,
        Guid documentId,
        ReservationDocumentKind kind,
        Guid attachedBy,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        if (documentId == Guid.Empty)
            throw new BadRequestException("El documento es obligatorio.", WheelbyErrorCodes.ReservationInvalidData);

        if (!Enum.IsDefined(kind))
            throw new BadRequestException("El tipo de documento no es válido.", WheelbyErrorCodes.ReservationInvalidData);

        if (!AttachableStates.Contains(reservation.Status))
            throw new ConflictException(
                $"No se pueden adjuntar documentos a una reserva en estado {reservation.Status}.",
                WheelbyErrorCodes.ReservationDocumentNotAllowed);

        return new ReservationDocument(reservation.Id, documentId, kind, attachedBy, utcNow);
    }
}
