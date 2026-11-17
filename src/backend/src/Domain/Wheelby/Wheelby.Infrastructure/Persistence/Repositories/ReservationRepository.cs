using Microsoft.EntityFrameworkCore;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;

namespace Wheelby.Infrastructure.Persistence;

internal sealed class ReservationRepository(WheelbyDbContext db) : IReservationRepository
{
    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.Reservations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
        => db.Reservations.AddAsync(reservation, cancellationToken).AsTask();

    public Task<bool> HasBlockingOverlapAsync(
        Guid vehicleId,
        DateTime start,
        DateTime end,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default)
    {
        // Arreglo local: EF Core no traduce IReadOnlySet.Contains a SQL.
        var blocking = BlockingReservationStates.States.ToArray();

        var query = db.Reservations
            .AsNoTracking()
            .Where(r => r.VehicleId == vehicleId
                && blocking.Contains(r.Status)
                && start < r.EndDate
                && end > r.StartDate);

        if (excludeReservationId is not null)
            query = query.Where(r => r.Id != excludeReservationId.Value);

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasBlockingReservationsAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var blocking = BlockingReservationStates.States.ToArray();
        return db.Reservations.AnyAsync(r => r.VehicleId == vehicleId && blocking.Contains(r.Status), cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> ListExpiredPendingAsync(
        DateTime utcNow, int take, CancellationToken cancellationToken)
        => await db.Reservations
            .Where(r => r.Status == ReservationStatus.Pending && r.StartDate <= utcNow)
            .OrderBy(r => r.StartDate).ThenBy(r => r.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountDocumentsAsync(Guid reservationId, CancellationToken cancellationToken)
        => db.ReservationDocuments.CountAsync(d => d.ReservationId == reservationId, cancellationToken);

    public Task<bool> DocumentAttachedAsync(Guid reservationId, Guid documentId, CancellationToken cancellationToken)
        => db.ReservationDocuments.AnyAsync(
            d => d.ReservationId == reservationId && d.DocumentId == documentId, cancellationToken);

    public Task AddDocumentAsync(ReservationDocument document, CancellationToken cancellationToken)
        => db.ReservationDocuments.AddAsync(document, cancellationToken).AsTask();
}
