using FluentValidation;

namespace Wheelby.Application.Features.StartRental;

internal sealed class StartRentalCommandValidator : AbstractValidator<StartRentalCommand>
{
    public StartRentalCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El id de la reserva es obligatorio.");
    }
}
