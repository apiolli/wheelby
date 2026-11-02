using FluentValidation;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Features.PublishVehicle;

// Valida solo la forma del comando (RD-07). Las invariantes de negocio las valida el dominio.
internal sealed class PublishVehicleCommandValidator : AbstractValidator<PublishVehicleCommand>
{
    public const int YearMin = 1980;

    public PublishVehicleCommandValidator()
    {
        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("La marca es obligatoria.")
            .MaximumLength(Vehicle.BrandMaxLength).WithMessage($"La marca no puede exceder los {Vehicle.BrandMaxLength} caracteres.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("El modelo es obligatorio.")
            .MaximumLength(Vehicle.ModelMaxLength).WithMessage($"El modelo no puede exceder los {Vehicle.ModelMaxLength} caracteres.");

        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(YearMin).WithMessage($"El año debe ser mayor o igual a {YearMin}.");

        RuleFor(x => x.Plate)
            .NotEmpty().WithMessage("La placa es obligatoria.");

        RuleFor(x => x.PricePerDay)
            .GreaterThan(0).WithMessage("El precio por día debe ser mayor que cero.")
            .LessThanOrEqualTo(Vehicle.PricePerDayMax).WithMessage($"El precio por día no puede exceder los {Vehicle.PricePerDayMax}.");
    }
}
