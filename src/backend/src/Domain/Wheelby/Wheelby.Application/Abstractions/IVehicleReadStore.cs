using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Abstractions;

// Puerto de lectura de vehículos disponibles.
public interface IVehicleReadStore
{
    Task<PagedResult<VehicleSummaryDTO>> SearchAvailableAsync(
        DateTime from,
        DateTime to,
        Guid excludeOwnerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
