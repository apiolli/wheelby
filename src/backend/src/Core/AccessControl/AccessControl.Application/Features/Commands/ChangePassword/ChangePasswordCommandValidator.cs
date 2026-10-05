using AccessControl.Application.Common;
using FluentValidation;

namespace AccessControl.Application.Features.ChangePassword;

internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("La contraseña actual es obligatoria.")
            .MaximumLength(PasswordRules.MaxLength).WithMessage($"La contraseña actual no puede pasar de {PasswordRules.MaxLength} caracteres.");

        RuleFor(x => x.NewPassword).MustBeValidPassword();
    }
}
