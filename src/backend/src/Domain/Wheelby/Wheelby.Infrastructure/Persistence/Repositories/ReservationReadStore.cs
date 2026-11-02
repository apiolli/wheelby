using Microsoft.EntityFrameworkCore;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Reservations;

namespace Wheelby.Infrastructure.Persistence;

internal sealed class ReservationReadStore(WheelbyDbContext db) : IReservationReadStore
{
    public Task<PagedResult<ReservationSummaryDTO>> ListByRenterAsync(
        Guid renterId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.Reservations
            .AsNoTracking()
            .Where(r => r.RenterId == renterId);
        return ToPageAsync(query, page, pageSize, cancellationToken);
    }

    public Task<PagedResult<ReservationSummaryDTO>> ListByHostAsync(
        Guid hostId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.Reservations
            .AsNoTracking()
            .Join(
                db.Vehicles,
                r => r.VehicleId,
                v => v.Id,
                (r, v) => new { r, v })
            .Where(x => x.v.OwnerId == hostId)
            .Select(x => x.r);
        return ToPageAsync(query, page, pageSize, cancellationToken);
    }

    public Task<PagedResult<ReservationSummaryDTO>> ListAllAsync(
        ReservationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.Reservations.AsNoTracking();
        if (status is not null)
            query = query.Where(r => r.Status == status);
        return ToPageAsync(query, page, pageSize, cancellationToken);
    }

    private static async Task<PagedResult<ReservationSummaryDTO>> ToPageAsync(
        IQueryable<Reservation> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReservationSummaryDTO(
                r.Id, r.VehicleId, r.RenterId, r.StartDate, r.EndDate, r.Status, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ReservationSummaryDTO>(items, page, pageSize, total);
    }
}
