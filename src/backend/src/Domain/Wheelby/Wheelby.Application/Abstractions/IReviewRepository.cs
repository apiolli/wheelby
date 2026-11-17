using Wheelby.Domain.Reviews;

namespace Wheelby.Application.Abstractions;

public interface IReviewRepository
{
    Task<bool> ExistsAsync(Guid reservationId, Guid authorId, CancellationToken cancellationToken = default);
    Task AddAsync(Review review, CancellationToken cancellationToken = default);
}
