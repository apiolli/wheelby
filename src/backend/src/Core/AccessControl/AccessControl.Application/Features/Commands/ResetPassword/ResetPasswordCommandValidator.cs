using AccessControl.Application.Common;
using FluentValidation;

namespace AccessControl.Application.Features.ResetPassword;

internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(Domain.Users.Email.MaxLength).WithMessage("El correo es demasiado largo.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código de recuperación es obligatorio.")
            .MaximumLength(200).WithMessage("El código de recuperación es demasiado largo.");

        RuleFor(x => x.NewPassword).MustBeValidPassword();
    }
}
