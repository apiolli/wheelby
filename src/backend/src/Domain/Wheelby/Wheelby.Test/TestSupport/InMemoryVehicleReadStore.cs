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
        string? city,
        Wheelby.Domain.Vehicles.VehicleType? type,
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
            .Select(ToDto)
            .ToList();

        var total = disponibles.Count;
        var items = disponibles.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PagedResult<VehicleSummaryDTO>(items, page, pageSize, total));
    }

    public Task<VehicleSummaryDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_vehicles
            .Where(v => v.Id == id)
            .Select(ToDto)
            .Cast<VehicleSummaryDTO?>()
            .FirstOrDefault());

    public Task<PagedResult<VehicleSummaryDTO>> ListByOwnerAsync(
        Guid ownerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var propios = _vehicles.Where(v => v.OwnerId == ownerId).Select(ToDto).ToList();
        var items = propios.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<VehicleSummaryDTO>(items, page, pageSize, propios.Count));
    }

    public Task<IReadOnlyList<BlockedRangeDTO>> ListBlockedRangesAsync(
        Guid vehicleId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BlockedRangeDTO> rangos = _reservations
            .Where(r => r.VehicleId == vehicleId
                && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.InProgress)
                && from < r.EndDate
                && to > r.StartDate)
            .OrderBy(r => r.StartDate)
            .Select(r => new BlockedRangeDTO(r.StartDate, r.EndDate))
            .ToList();
        return Task.FromResult(rangos);
    }

    private static VehicleSummaryDTO ToDto((Guid Id, Guid OwnerId, string Plate) v)
        => new(v.Id, v.OwnerId, "Marca", "Modelo", 2020, v.Plate, 100m, DateTime.UtcNow);

    public void AgregarVehiculo(Guid id, Guid ownerId, string plate)
        => _vehicles.Add((id, ownerId, plate));

    public void AgregarReserva(Guid vehicleId, ReservationStatus status, DateTime start, DateTime end)
        => _reservations.Add((vehicleId, status, start, end));

    public Task<IReadOnlyList<VehiclePhotoDTO>> ListPhotosAsync(Guid vehicleId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<VehiclePhotoDTO>>([]);

    public List<ReviewDTO> Resenas { get; } = [];

    public Task<PagedResult<ReviewDTO>> ListReviewsAsync(Guid vehicleId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = Resenas.Where(r => r.VehicleId == vehicleId && r.AuthorRole == ReservationActor.Renter).ToList();
        return Task.FromResult(new PagedResult<ReviewDTO>(items, page, pageSize, items.Count));
    }
}
