using Microsoft.EntityFrameworkCore;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;

namespace Wheelby.Infrastructure.Persistence;

// RF-REP-03 (CA-REP-03.2): join, GroupBy, Count y Sum traducidos a SQL por EF.
// A memoria solo llega una fila por vehículo, nunca las reservas.
internal sealed class VehicleRevenueReportStore(WheelbyDbContext db) : IVehicleRevenueReportStore
{
    // Pending y Cancelled no generan ingreso. Arreglo, no IReadOnlySet (AGENTS.md §8).
    private static readonly ReservationStatus[] RevenueStatuses =
        [ReservationStatus.Confirmed, ReservationStatus.InProgress, ReservationStatus.Completed];

    public async Task<IReadOnlyList<VehicleRevenueGroup>> SumByVehicleAsync(
        DateTime startUtc, DateTime endUtc, Guid? ownerId, CancellationToken cancellationToken)
    {
        var statuses = RevenueStatuses;

        var query =
            from r in db.Reservations.AsNoTracking()
            join v in db.Vehicles.AsNoTracking() on r.VehicleId equals v.Id
            where statuses.Contains(r.Status) && r.StartDate >= startUtc && r.StartDate < endUtc
            select new { r.VehicleId, v.OwnerId, v.Brand, v.Model, v.Plate, r.Days, r.TotalAmount };

        if (ownerId is not null)
            query = query.Where(x => x.OwnerId == ownerId);

        var groups = await query
            .GroupBy(x => new { x.VehicleId, x.Brand, x.Model, x.Plate })
            .Select(g => new VehicleRevenueGroup(
                g.Key.VehicleId,
                g.Key.Brand,
                g.Key.Model,
                g.Key.Plate,
                g.Count(),
                g.Sum(x => x.Days),
                g.Sum(x => x.TotalAmount)))
            .ToListAsync(cancellationToken);

        return groups;
    }
}
