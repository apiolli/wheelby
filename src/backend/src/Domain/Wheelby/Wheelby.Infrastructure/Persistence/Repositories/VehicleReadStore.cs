using Microsoft.EntityFrameworkCore;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Reservations;

namespace Wheelby.Infrastructure.Persistence;

internal sealed class VehicleReadStore(WheelbyDbContext db) : IVehicleReadStore
{
    public async Task<PagedResult<VehicleSummaryDTO>> SearchAvailableAsync(
        DateTime from,
        DateTime to,
        Guid excludeOwnerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.Vehicles
            .AsNoTracking()
            .Where(v => v.OwnerId != excludeOwnerId
                && !db.Reservations
                    .Where(r => r.VehicleId == v.Id
                        && BlockingReservationStates.States.Contains(r.Status)
                        && from < r.EndDate
                        && to > r.StartDate)
                    .Any());

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(v => v.CreatedAt).ThenBy(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VehicleSummaryDTO(
                v.Id, v.OwnerId, v.Brand, v.Model, v.Year, v.Plate, v.PricePerDay, v.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<VehicleSummaryDTO>(items, page, pageSize, total);
    }
}
