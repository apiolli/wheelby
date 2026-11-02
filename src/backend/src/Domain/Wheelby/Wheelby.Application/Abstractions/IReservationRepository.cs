using Wheelby.Domain.Reservations;

namespace Wheelby.Application.Abstractions;

// Puerto de escritura del agregado Reservation.
public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task<bool> HasBlockingOverlapAsync(
        Guid vehicleId,
        DateTime start,
        DateTime end,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default);
}
