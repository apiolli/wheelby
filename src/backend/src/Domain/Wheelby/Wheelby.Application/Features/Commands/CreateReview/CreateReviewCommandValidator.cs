using FluentValidation;
using Wheelby.Domain.Reviews;

namespace Wheelby.Application.Features.CreateReview;

internal sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty().WithMessage("El id de la reserva es obligatorio.");
        RuleFor(x => x.Rating).InclusiveBetween(Review.RatingMin, Review.RatingMax)
            .WithMessage($"La calificación debe estar entre {Review.RatingMin} y {Review.RatingMax}.");
        RuleFor(x => x.Comment).MaximumLength(Review.CommentMaxLength)
            .WithMessage($"El comentario no puede exceder los {Review.CommentMaxLength} caracteres.");
    }
}
