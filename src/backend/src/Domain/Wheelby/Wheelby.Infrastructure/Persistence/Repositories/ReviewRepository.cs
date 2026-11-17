using Microsoft.EntityFrameworkCore;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reviews;

namespace Wheelby.Infrastructure.Persistence;

internal sealed class ReviewRepository(WheelbyDbContext db) : IReviewRepository
{
    public Task<bool> ExistsAsync(Guid reservationId, Guid authorId, CancellationToken cancellationToken)
        => db.Reviews.AnyAsync(r => r.ReservationId == reservationId && r.AuthorId == authorId, cancellationToken);

    public Task AddAsync(Review review, CancellationToken cancellationToken)
        => db.Reviews.AddAsync(review, cancellationToken).AsTask();
}
