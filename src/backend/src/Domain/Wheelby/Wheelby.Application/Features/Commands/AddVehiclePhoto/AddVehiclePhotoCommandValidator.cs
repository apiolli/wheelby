using FluentValidation;

namespace Wheelby.Application.Features.AddVehiclePhoto;

internal sealed class AddVehiclePhotoCommandValidator : AbstractValidator<AddVehiclePhotoCommand>
{
    public AddVehiclePhotoCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage("El id del vehículo es obligatorio.");
        RuleFor(x => x.DocumentId).NotEmpty().WithMessage("El documento es obligatorio.");
    }
}
