using FluentValidation;

namespace Wheelby.Application.Features.SearchAvailableVehicles;

// Valida solo la forma de la consulta (RD-07).
internal sealed class SearchAvailableVehiclesQueryValidator : AbstractValidator<SearchAvailableVehiclesQuery>
{
    public const int MaxDays = 365;
    public const int MaxPageSize = 100;

    public SearchAvailableVehiclesQueryValidator()
    {
        RuleFor(x => x.From)
            .NotEmpty().WithMessage("La fecha de inicio es obligatoria.");

        RuleFor(x => x.To)
            .NotEmpty().WithMessage("La fecha de fin es obligatoria.");

        RuleFor(x => x)
            .Must(x => x.To > x.From)
            .WithMessage("La fecha de fin debe ser posterior a la de inicio.");

        RuleFor(x => x)
            .Must(x => (x.To - x.From).TotalDays <= MaxDays)
            .WithMessage($"El rango de fechas no puede exceder los {MaxDays} días.");

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {MaxPageSize}.");
    }
}
