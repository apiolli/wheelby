using FluentValidation;

namespace Wheelby.Application.Features.CancelReservation;

internal sealed class CancelReservationCommandValidator : AbstractValidator<CancelReservationCommand>
{
    public CancelReservationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El id de la reserva es obligatorio.");
    }
}
