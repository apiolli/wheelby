using Wheelby.Domain.Reservations;

namespace Wheelby.Application.Abstractions;

// Puerto de escritura del agregado Reservation y de sus documentos.
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

    // ¿Tiene el vehículo alguna reserva Confirmed o InProgress (en cualquier fecha)?
    Task<bool> HasBlockingReservationsAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    // Solicitudes Pending cuyo inicio ya llegó, para el job de expiración.
    Task<IReadOnlyList<Reservation>> ListExpiredPendingAsync(
        DateTime utcNow,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountDocumentsAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task<bool> DocumentAttachedAsync(Guid reservationId, Guid documentId, CancellationToken cancellationToken = default);
    Task AddDocumentAsync(ReservationDocument document, CancellationToken cancellationToken = default);
}
