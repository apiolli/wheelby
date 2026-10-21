using FluentValidation;
using Permissions.Domain.PermissionRequests;

namespace Permissions.Application.Features.CreatePermissionRequest;

internal sealed class CreatePermissionRequestCommandValidator : AbstractValidator<CreatePermissionRequestCommand>
{
    public CreatePermissionRequestCommandValidator()
    {
        RuleFor(x => x.PermissionCode)
            .NotEmpty().WithMessage("El código de permiso es obligatorio.")
            .Must(PermissionCodeRules.IsValid)
            .WithMessage(
                $"El código de permiso debe tener un formato válido " +
                $"(minúsculas, dígitos, puntos y guiones; máximo {PermissionCodeRules.MaxLength} caracteres).");
    }
}
