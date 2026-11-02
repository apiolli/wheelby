using FluentValidation;

namespace Wheelby.Application.Features.RequestReservation;

// Valida solo la forma del comando (RD-07). El dominio valida fechas e invariantes.
internal sealed class RequestReservationCommandValidator : AbstractValidator<RequestReservationCommand>
{
    public RequestReservationCommandValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("El vehículo es obligatorio.");

        RuleFor(x => x.Start)
            .NotEmpty().WithMessage("La fecha de inicio es obligatoria.");

        RuleFor(x => x.End)
            .NotEmpty().WithMessage("La fecha de fin es obligatoria.");

        RuleFor(x => x)
            .Must(x => x.End > x.Start)
            .WithMessage("La fecha de fin debe ser posterior a la de inicio.");
    }
}
