using FluentValidation;
namespace AccessControl.Application.Features.EnableUser;

internal sealed class EnableUserCommandValidator : AbstractValidator<EnableUserCommand>
{
    public EnableUserCommandValidator()
        => RuleFor(x => x.UserId).NotEmpty().WithMessage("El usuario es obligatorio.");
}
