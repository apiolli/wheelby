using Wheelby.Domain.Reservations;

namespace Wheelby.Application.DTOs;

// Fila de la línea de tiempo del detalle. From es null en la creación;
// ActorId es null cuando actuó el sistema (expiración).
public sealed record ReservationStatusChangeDTO(
    ReservationStatus? From,
    ReservationStatus To,
    ReservationActor Actor,
    Guid? ActorId,
    string? Reason,
    DateTime At);

// Documento adjunto a la reserva. FileName, ContentType y SizeBytes vienen de
// Documents; son null si el documento se eliminó después de adjuntarlo.
public sealed record ReservationDocumentDTO(
    Guid DocumentId,
    ReservationDocumentKind Kind,
    Guid AttachedBy,
    DateTime AttachedAt,
    string? FileName = null,
    string? ContentType = null,
    long? SizeBytes = null);

public sealed record ReviewDTO(
    Guid Id,
    Guid ReservationId,
    Guid VehicleId,
    Guid AuthorId,
    ReservationActor AuthorRole,
    Guid SubjectId,
    int Rating,
    string? Comment,
    DateTime CreatedAt);

public sealed record VehiclePhotoDTO(Guid DocumentId, DateTime AddedAt);

// Archivo para responder con Results.File (fotos y documentos de la reserva).
public sealed record FileContentDTO(Stream Content, string ContentType, string FileName);
