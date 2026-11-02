using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;

namespace Wheelby.Test.TestSupport;

// Repositorio en memoria de reservas para pruebas unitarias de casos de uso.
public sealed class InMemoryReservationRepository : IReservationRepository
{
    private readonly List<Reservation> _reservations = [];

    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_reservations.FirstOrDefault(r => r.Id == id));

    public Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        _reservations.Add(reservation);
        return Task.CompletedTask;
    }

    public Task<bool> HasBlockingOverlapAsync(
        Guid vehicleId,
        DateTime start,
        DateTime end,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default)
    {
        var overlap = _reservations.Any(r =>
            r.VehicleId == vehicleId
            && r.Id != excludeReservationId
            && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.InProgress)
            && start < r.EndDate
            && end > r.StartDate);

        return Task.FromResult(overlap);
    }

    public IReadOnlyList<Reservation> Reservations => _reservations.AsReadOnly();
}
