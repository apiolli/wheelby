using FluentValidation;

namespace AccessControl.Application.Features.RequestPasswordReset;

internal sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(Domain.Users.Email.MaxLength).WithMessage("El correo es demasiado largo.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.");
    }
}
