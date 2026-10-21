using FluentValidation;

namespace Permissions.Application.Features.ListPendingRequests;

internal sealed class ListPendingRequestsQueryValidator : AbstractValidator<ListPendingRequestsQuery>
{
    public const int MaxPageSize = 100;

    public ListPendingRequestsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {MaxPageSize}.");
    }
}
