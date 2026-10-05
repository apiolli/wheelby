using AccessControl.Contracts;
using FluentValidation;


namespace AccessControl.Application.Features.ChangeRole;

internal sealed class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("El usuario es obligatorio.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("El rol es obligatorio.")
            .Must(r => RoleParser.TryParse(r, out _))
            .WithMessage($"El rol debe ser {AccessControlRoles.Administrator} o {AccessControlRoles.Standard}.");
    }
}
