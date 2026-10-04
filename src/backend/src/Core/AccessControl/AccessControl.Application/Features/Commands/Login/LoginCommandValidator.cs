using AccessControl.Application.Common;
using Accessontrol.Application.Features.Login;
using FluentValidation;


namespace AccessControl.Application.Features.Login;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(Domain.Users.Email.MaxLength).WithMessage("El correo es demasiado largo.");
            
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MaximumLength(PasswordRules.MaxLength).WithMessage("La contraseña es demasiado larga.");
    }
}

