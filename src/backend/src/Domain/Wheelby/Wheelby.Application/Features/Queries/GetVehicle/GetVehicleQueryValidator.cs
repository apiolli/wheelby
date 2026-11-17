using FluentValidation;

namespace Wheelby.Application.Features.GetVehicle;

internal sealed class GetVehicleQueryValidator : AbstractValidator<GetVehicleQuery>
{
    public GetVehicleQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El id del vehículo es obligatorio.");
    }
}
