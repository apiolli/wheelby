using FluentValidation;

namespace Wheelby.Application.Features.GetVehicleAvailability;

internal sealed class GetVehicleAvailabilityQueryValidator : AbstractValidator<GetVehicleAvailabilityQuery>
{
    public const int MaxDays = 366;

    public GetVehicleAvailabilityQueryValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage("El id del vehículo es obligatorio.");

        RuleFor(x => x.From).NotEmpty().WithMessage("La fecha de inicio es obligatoria.");
        RuleFor(x => x.To).NotEmpty().WithMessage("La fecha de fin es obligatoria.");

        RuleFor(x => x)
            .Must(x => x.To > x.From)
            .WithName("To")
            .WithMessage("La fecha de fin debe ser posterior a la de inicio.");

        RuleFor(x => x)
            .Must(x => (x.To - x.From).TotalDays <= MaxDays)
            .WithName("To")
            .WithMessage($"El rango de fechas no puede exceder los {MaxDays} días.");
    }
}
