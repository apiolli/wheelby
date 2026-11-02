using FluentValidation;

namespace Wheelby.Application.Features.ListAllReservations;

internal sealed class ListAllReservationsQueryValidator : AbstractValidator<ListAllReservationsQuery>
{
    public const int MaxPageSize = 100;

    public ListAllReservationsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {MaxPageSize}.");
    }
}
