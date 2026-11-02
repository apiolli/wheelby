using FluentValidation;

namespace Wheelby.Application.Features.CompleteRental;

internal sealed class CompleteRentalCommandValidator : AbstractValidator<CompleteRentalCommand>
{
    public CompleteRentalCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El id de la reserva es obligatorio.");
    }
}
