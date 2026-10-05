using FluentValidation;

namespace AccessControl.Application.Features.ForcePasswordReset;

internal sealed class ForcePasswordResetCommandValidator : AbstractValidator<ForcePasswordResetCommand>
{
    public ForcePasswordResetCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El usuario es obligatorio.");
    }
}
