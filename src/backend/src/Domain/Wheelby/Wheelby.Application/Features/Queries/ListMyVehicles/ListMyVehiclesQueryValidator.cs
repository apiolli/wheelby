using FluentValidation;
using Wheelby.Application.Common;

namespace Wheelby.Application.Features.ListMyVehicles;

internal sealed class ListMyVehiclesQueryValidator : AbstractValidator<ListMyVehiclesQuery>
{
    public ListMyVehiclesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paging.MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {Paging.MaxPageSize}.");
    }
}
