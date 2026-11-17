using Microsoft.EntityFrameworkCore;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Infrastructure.Persistence;

internal sealed class ReservationReadStore(WheelbyDbContext db) : IReservationReadStore
{
    public Task<ReservationSummaryDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Project(WithVehicle().Where(x => x.Reservation.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PagedResult<ReservationSummaryDTO>> ListByRenterAsync(
        Guid renterId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = WithVehicle().Where(x => x.Reservation.RenterId == renterId);
        return ToPageAsync(query, page, pageSize, cancellationToken);
    }

    public Task<PagedResult<ReservationSummaryDTO>> ListByHostAsync(
        Guid hostId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = WithVehicle().Where(x => x.Vehicle.OwnerId == hostId);
        return ToPageAsync(query, page, pageSize, cancellationToken);
    }

    public Task<PagedResult<ReservationSummaryDTO>> ListAllAsync(
        ReservationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = WithVehicle();
        if (status is not null)
            query = query.Where(x => x.Reservation.Status == status);
        return ToPageAsync(query, page, pageSize, cancellationToken);
    }

    // Toda reserva tiene vehículo (se valida al crearla), así que el join interno no pierde filas.
    private IQueryable<ReservationWithVehicle> WithVehicle()
        => db.Reservations
            .AsNoTracking()
            .Join(
                db.Vehicles.AsNoTracking(),
                r => r.VehicleId,
                v => v.Id,
                (r, v) => new ReservationWithVehicle { Reservation = r, Vehicle = v });

    private static IQueryable<ReservationSummaryDTO> Project(IQueryable<ReservationWithVehicle> query)
        => query.Select(x => new ReservationSummaryDTO(
            x.Reservation.Id,
            x.Reservation.VehicleId,
            x.Reservation.RenterId,
            x.Vehicle.OwnerId,
            x.Vehicle.Brand,
            x.Vehicle.Model,
            x.Vehicle.Year,
            x.Vehicle.Plate,
            x.Reservation.StartDate,
            x.Reservation.EndDate,
            x.Reservation.Status,
            x.Reservation.PricePerDayAtBooking,
            x.Reservation.Days,
            x.Reservation.TotalAmount,
            x.Reservation.CancellationReason,
            x.Reservation.CreatedAt,
            "DOP"));

    private static async Task<PagedResult<ReservationSummaryDTO>> ToPageAsync(
        IQueryable<ReservationWithVehicle> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);

        var items = await Project(query
                .OrderByDescending(x => x.Reservation.CreatedAt).ThenBy(x => x.Reservation.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<ReservationSummaryDTO>(items, page, pageSize, total);
    }

    private sealed class ReservationWithVehicle
    {
        public required Reservation Reservation { get; init; }
        public required Vehicle Vehicle { get; init; }
    }
}
