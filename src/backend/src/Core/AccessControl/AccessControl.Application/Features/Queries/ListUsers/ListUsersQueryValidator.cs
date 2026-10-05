using FluentValidation;

namespace AccessControl.Application.Features.ListUsers;

internal sealed class ListUsersQueryValidator : AbstractValidator<ListUsersQuery>
{
    public const int MaxPageSize = 100;

    public ListUsersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {MaxPageSize}.");
    }
}
