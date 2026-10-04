using FluentValidation;

namespace AccessControl.Application.Features.Activate;

internal sealed class ActivateAccountCommandValidator : AbstractValidator<ActivateAccountCommand>
{
    public ActivateAccountCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("El enlace de activación no es válido.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("El enlace de activación no es válido.");
    }
}
