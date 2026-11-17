using Wheelby.Application.Common;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Abstractions;

// Puerto de lectura de vehículos.
public interface IVehicleReadStore
{
    Task<PagedResult<VehicleSummaryDTO>> SearchAvailableAsync(
        DateTime from,
        DateTime to,
        Guid excludeOwnerId,
        string? city,
        VehicleType? type,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<VehicleSummaryDTO?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<VehicleSummaryDTO>> ListByOwnerAsync(
        Guid ownerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehiclePhotoDTO>> ListPhotosAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    // Reseñas que los inquilinos dejaron sobre el vehículo, las más nuevas primero.
    Task<PagedResult<ReviewDTO>> ListReviewsAsync(
        Guid vehicleId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    // Reservas bloqueantes que tocan [from, to), ordenadas por inicio.
    Task<IReadOnlyList<BlockedRangeDTO>> ListBlockedRangesAsync(
        Guid vehicleId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
