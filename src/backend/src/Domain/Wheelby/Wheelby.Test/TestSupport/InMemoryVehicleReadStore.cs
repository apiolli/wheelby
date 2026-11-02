using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Reservations;

namespace Wheelby.Test.TestSupport;

// Doble de IVehicleReadStore para pruebas de búsqueda (F2).
public sealed class InMemoryVehicleReadStore : IVehicleReadStore
{
    private readonly List<(Guid Id, Guid OwnerId, string Plate)> _vehicles = [];
    private readonly List<(Guid VehicleId, ReservationStatus Status, DateTime StartDate, DateTime EndDate)> _reservations = [];

    public Task<PagedResult<VehicleSummaryDTO>> SearchAvailableAsync(
        DateTime from,
        DateTime to,
        Guid excludeOwnerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var disponibles = _vehicles
            .Where(v => v.OwnerId != excludeOwnerId)
            .Where(v => !_reservations.Any(r =>
                r.VehicleId == v.Id
                && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.InProgress)
                && from < r.EndDate
                && to > r.StartDate))
            .Select(v => new VehicleSummaryDTO(v.Id, v.OwnerId, "Marca", "Modelo", 2020, v.Plate, 100m, DateTime.UtcNow))
            .ToList();

        var total = disponibles.Count;
        var items = disponibles.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PagedResult<VehicleSummaryDTO>(items, page, pageSize, total));
    }

    public void AgregarVehiculo(Guid id, Guid ownerId, string plate)
        => _vehicles.Add((id, ownerId, plate));

    public void AgregarReserva(Guid vehicleId, ReservationStatus status, DateTime start, DateTime end)
        => _reservations.Add((vehicleId, status, start, end));
}
