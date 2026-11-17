using FluentValidation;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Features.UpdateVehicle;

internal sealed class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El id del vehículo es obligatorio.");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("La marca es obligatoria.")
            .MaximumLength(Vehicle.BrandMaxLength).WithMessage($"La marca no puede exceder los {Vehicle.BrandMaxLength} caracteres.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("El modelo es obligatorio.")
            .MaximumLength(Vehicle.ModelMaxLength).WithMessage($"El modelo no puede exceder los {Vehicle.ModelMaxLength} caracteres.");

        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(Vehicle.YearMin).WithMessage($"El año debe ser mayor o igual a {Vehicle.YearMin}.");

        RuleFor(x => x.Plate).NotEmpty().WithMessage("La placa es obligatoria.");

        RuleFor(x => x.PricePerDay)
            .GreaterThan(0).WithMessage("El precio por día debe ser mayor que cero.")
            .LessThanOrEqualTo(Vehicle.PricePerDayMax).WithMessage($"El precio por día no puede exceder los {Vehicle.PricePerDayMax}.");

        RuleFor(x => x.Type).IsInEnum().WithMessage("El tipo debe ser Car, Motorcycle, Bicycle, Scooter u Other.");

        RuleFor(x => x.City).MaximumLength(Vehicle.CityMaxLength)
            .WithMessage($"La ciudad no puede exceder los {Vehicle.CityMaxLength} caracteres.");

        RuleFor(x => x.Description).MaximumLength(Vehicle.DescriptionMaxLength)
            .WithMessage($"La descripción no puede exceder los {Vehicle.DescriptionMaxLength} caracteres.");
    }
}
