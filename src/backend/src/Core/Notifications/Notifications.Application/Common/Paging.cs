using FluentValidation;

namespace Notifications.Application.Common;

internal static class Paging
{
    public const int MaxPageSize = 100;

    public static void AddPagingRules<T>(this AbstractValidator<T> validator, Func<T, int> page, Func<T, int> pageSize)
    {
        validator.RuleFor(x => page(x)).GreaterThanOrEqualTo(1)
            .OverridePropertyName("page")
            .WithMessage("La página debe ser 1 o mayor.");
        validator.RuleFor(x => pageSize(x)).InclusiveBetween(1, MaxPageSize)
            .OverridePropertyName("pageSize")
            .WithMessage($"El tamaño de página debe estar entre 1 y {MaxPageSize}.");
    }
}
