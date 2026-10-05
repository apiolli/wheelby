using FluentValidation;

namespace AccessControl.Application.Features.DisableUser;

internal sealed class DisableUserCommandValidator : AbstractValidator<DisableUserCommand>
{
    public DisableUserCommandValidator()
        => RuleFor(x => x.UserId).NotEmpty().WithMessage("El usuario es obligatorio.");
}
