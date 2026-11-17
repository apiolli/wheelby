using Shared.Application.Abstractions;
using Wheelby.Domain.Reservations;

namespace Wheelby.Application.Features.AttachReservationDocument;

// El documento se sube antes por POST /documents; aquí solo se vincula su id.
public sealed record AttachReservationDocumentCommand(
    Guid ReservationId,
    Guid DocumentId,
    ReservationDocumentKind Kind) : ICommand;
