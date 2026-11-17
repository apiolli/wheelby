using Shared.Application.Abstractions;

namespace Wheelby.Application.Features.CreateReview;

public sealed record CreateReviewCommand(Guid ReservationId, int Rating, string? Comment) : ICommand<Guid>;
