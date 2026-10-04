using AccessControl.Application.Common;
using AccessControl.Domain.Users;
using FluentValidation;

namespace AccessControl.Application.Features.Register;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .Must(n => n is null || (n.Trim().Length >= User.FullNameMinLength
                                     && n.Trim().Length <= User.FullNameMaxLength))
            .WithMessage($"El nombre debe tener entre {User.FullNameMinLength} y {User.FullNameMaxLength} caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(Email.MaxLength).WithMessage("El correo es demasiado largo.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.");

        RuleFor(x => x.Password).MustBeValidPassword();
    }
}
