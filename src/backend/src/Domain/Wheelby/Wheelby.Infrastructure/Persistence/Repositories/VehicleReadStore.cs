using Microsoft.EntityFrameworkCore;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Infrastructure.Persistence;

internal sealed class VehicleReadStore(WheelbyDbContext db) : IVehicleReadStore
{
    public Task<PagedResult<VehicleSummaryDTO>> SearchAvailableAsync(
        DateTime from,
        DateTime to,
        Guid excludeOwnerId,
        string? city,
        VehicleType? type,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Arreglo local: EF Core no traduce IReadOnlySet.Contains a SQL.
        var blocking = BlockingReservationStates.States.ToArray();

        var query = db.Vehicles
            .AsNoTracking()
            .Where(v => v.IsActive
                && v.OwnerId != excludeOwnerId
                && !db.Reservations
                    .Where(r => r.VehicleId == v.Id
                        && blocking.Contains(r.Status)
                        && from < r.EndDate
                        && to > r.StartDate)
                    .Any());

        if (city is not null)
        {
            var normalizedCity = city.ToLower();
            query = query.Where(v => v.City != null && v.City.ToLower() == normalizedCity);
        }

        if (type is not null)
            query = query.Where(v => v.Type == type);

        return ToPageAsync(
            query.OrderBy(v => v.CreatedAt).ThenBy(v => v.Id),
            page, pageSize, cancellationToken);
    }

    public Task<VehicleSummaryDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Project(db.Vehicles.AsNoTracking().Where(v => v.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PagedResult<VehicleSummaryDTO>> ListByOwnerAsync(
        Guid ownerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.Vehicles
            .AsNoTracking()
            .Where(v => v.OwnerId == ownerId)
            .OrderByDescending(v => v.CreatedAt).ThenBy(v => v.Id);

        return ToPageAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<VehiclePhotoDTO>> ListPhotosAsync(Guid vehicleId, CancellationToken cancellationToken)
        => await db.VehiclePhotos
            .AsNoTracking()
            .Where(p => p.VehicleId == vehicleId)
            .OrderBy(p => p.AddedAt).ThenBy(p => p.Id)
            .Select(p => new VehiclePhotoDTO(p.DocumentId, p.AddedAt))
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<ReviewDTO>> ListReviewsAsync(
        Guid vehicleId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Reviews
            .AsNoTracking()
            .Where(r => r.VehicleId == vehicleId && r.AuthorRole == ReservationActor.Renter);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReviewDTO(
                r.Id, r.ReservationId, r.VehicleId, r.AuthorId, r.AuthorRole, r.SubjectId, r.Rating, r.Comment, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ReviewDTO>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<BlockedRangeDTO>> ListBlockedRangesAsync(
        Guid vehicleId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var blocking = BlockingReservationStates.States.ToArray();

        // Mismo criterio de solape que HasBlockingOverlapAsync.
        return await db.Reservations
            .AsNoTracking()
            .Where(r => r.VehicleId == vehicleId
                && blocking.Contains(r.Status)
                && from < r.EndDate
                && to > r.StartDate)
            .OrderBy(r => r.StartDate)
            .Select(r => new BlockedRangeDTO(r.StartDate, r.EndDate))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<VehicleSummaryDTO> Project(IQueryable<Vehicle> query)
        => query.Select(v => new VehicleSummaryDTO(
            v.Id, v.OwnerId, v.Brand, v.Model, v.Year, v.Plate, v.PricePerDay, v.CreatedAt,
            "DOP", null, null, null, v.Type, v.City, v.Description, v.IsActive,
            db.VehiclePhotos
                .Where(p => p.VehicleId == v.Id)
                .OrderBy(p => p.AddedAt).ThenBy(p => p.Id)
                .Select(p => (Guid?)p.DocumentId)
                .FirstOrDefault()));

    private async Task<PagedResult<VehicleSummaryDTO>> ToPageAsync(
        IOrderedQueryable<Vehicle> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);

        var items = await Project(query
                .Skip((page - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<VehicleSummaryDTO>(items, page, pageSize, total);
    }
}
