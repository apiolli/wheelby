using AccessControl.Application.Common;
using FluentValidation;

namespace AccessControl.Application.Features.EnsureAdministrator;

internal sealed class EnsureAdministratorCommandValidator : AbstractValidator<EnsureAdministratorCommand>
{
    public EnsureAdministratorCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("El nombre del administrador es obligatorio.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo del administrador es obligatorio.")
            .EmailAddress().WithMessage("El correo del administrador no tiene un formato válido.");
        RuleFor(x => x.Password).MustBeValidPassword();
    }
}
