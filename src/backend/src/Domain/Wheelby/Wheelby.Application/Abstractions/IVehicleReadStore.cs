using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Abstractions;

// Puerto de lectura de vehículos.
public interface IVehicleReadStore
{
    Task<PagedResult<VehicleSummaryDTO>> SearchAvailableAsync(
        DateTime from,
        DateTime to,
        Guid excludeOwnerId,
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

    // Reservas bloqueantes que tocan [from, to), ordenadas por inicio.
    Task<IReadOnlyList<BlockedRangeDTO>> ListBlockedRangesAsync(
        Guid vehicleId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
