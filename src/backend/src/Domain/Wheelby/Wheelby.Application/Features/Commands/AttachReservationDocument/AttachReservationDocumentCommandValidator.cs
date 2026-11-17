using FluentValidation;

namespace Wheelby.Application.Features.AttachReservationDocument;

internal sealed class AttachReservationDocumentCommandValidator : AbstractValidator<AttachReservationDocumentCommand>
{
    public AttachReservationDocumentCommandValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty().WithMessage("El id de la reserva es obligatorio.");
        RuleFor(x => x.DocumentId).NotEmpty().WithMessage("El documento es obligatorio.");
        RuleFor(x => x.Kind).IsInEnum().WithMessage("El tipo debe ser Delivery (entrega) o Return (devolución).");
    }
}
