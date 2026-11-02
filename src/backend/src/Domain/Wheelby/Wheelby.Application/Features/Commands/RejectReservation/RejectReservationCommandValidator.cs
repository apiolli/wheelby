using FluentValidation;
using Wheelby.Domain.Reservations;

namespace Wheelby.Application.Features.RejectReservation;

internal sealed class RejectReservationCommandValidator : AbstractValidator<RejectReservationCommand>
{
    public RejectReservationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El id de la reserva es obligatorio.");
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("El motivo es obligatorio para rechazar una reserva.")
            .MaximumLength(Reservation.CancellationReasonMaxLength).WithMessage($"El motivo no puede exceder los {Reservation.CancellationReasonMaxLength} caracteres.");
    }
}
