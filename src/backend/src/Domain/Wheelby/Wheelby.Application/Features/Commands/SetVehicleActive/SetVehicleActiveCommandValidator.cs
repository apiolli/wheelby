using FluentValidation;

namespace Wheelby.Application.Features.SetVehicleActive;

internal sealed class SetVehicleActiveCommandValidator : AbstractValidator<SetVehicleActiveCommand>
{
    public SetVehicleActiveCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El id del vehículo es obligatorio.");
    }
}
