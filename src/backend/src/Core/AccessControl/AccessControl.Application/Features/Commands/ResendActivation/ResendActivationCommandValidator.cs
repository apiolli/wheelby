using FluentValidation;

namespace AccessControl.Application.Features.ResendActivation;

internal sealed class ResendActivationCommandValidator : AbstractValidator<ResendActivationCommand>
{
    public ResendActivationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(Domain.Users.Email.MaxLength).WithMessage("El correo es demasiado largo.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.");
    }
}
