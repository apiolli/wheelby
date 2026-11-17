using FluentValidation;
using Wheelby.Application.Common;

namespace Wheelby.Application.Features.ListVehicleReviews;

internal sealed class ListVehicleReviewsQueryValidator : AbstractValidator<ListVehicleReviewsQuery>
{
    public ListVehicleReviewsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paging.MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {Paging.MaxPageSize}.");
    }
}
