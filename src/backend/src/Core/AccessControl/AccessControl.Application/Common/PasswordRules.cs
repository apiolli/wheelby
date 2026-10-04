using FluentValidation;

namespace AccessControl.Application.Common;

internal static class PasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static IRuleBuilderOptions<T, string> MustBeValidPassword<T>(
        this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(MinLength).WithMessage($"La contraseña debe tener al menos {MinLength} caracteres.")
            .MaximumLength(MaxLength).WithMessage($"La contraseña no puede pasar de {MaxLength} caracteres.")
            .Must(p => !string.IsNullOrEmpty(p) && p.Any(char.IsLetter) && p.Any(char.IsDigit))
            .WithMessage("La contraseña debe contener letras y números.");
}