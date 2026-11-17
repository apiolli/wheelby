using FluentValidation;

namespace Wheelby.Application.Features.GetReservation;

internal sealed class GetReservationQueryValidator : AbstractValidator<GetReservationQuery>
{
    public GetReservationQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El id de la reserva es obligatorio.");
    }
}
