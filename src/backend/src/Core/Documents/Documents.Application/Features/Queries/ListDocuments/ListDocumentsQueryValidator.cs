using FluentValidation;

namespace Documents.Application.Features.ListDocuments;

internal sealed class ListDocumentsQueryValidator : AbstractValidator<ListDocumentsQuery>
{
    public const int MaxPageSize = 100;

    public ListDocumentsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {MaxPageSize}.");
    }
}
