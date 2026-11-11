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

        // Código ISO 4217: exactamente 3 letras. El endpoint lo normaliza a mayúsculas.
        RuleFor(x => x.Currency)
            .Matches("^[A-Z]{3}$").When(x => x.Currency is not null)
            .WithMessage("La moneda debe ser un código de 3 letras, por ejemplo USD.");

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {MaxPageSize}.");
    }
}
