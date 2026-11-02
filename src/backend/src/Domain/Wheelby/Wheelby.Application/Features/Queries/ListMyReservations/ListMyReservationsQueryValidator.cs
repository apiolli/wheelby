using FluentValidation;

namespace Wheelby.Application.Features.ListMyReservations;

internal sealed class ListMyReservationsQueryValidator : AbstractValidator<ListMyReservationsQuery>
{
    public const int MaxPageSize = 100;

    public ListMyReservationsQueryValidator()
    {
        RuleFor(x => x.As)
            .Must(x => x == "renter" || x == "host")
            .WithMessage("El valor de 'as' debe ser 'renter' o 'host'.");

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {MaxPageSize}.");
    }
}
