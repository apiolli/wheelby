using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Reservations;

namespace Wheelby.Test.TestSupport;

// Doble de IReservationReadStore para pruebas de listados (F6).
public sealed class InMemoryReservationReadStore : IReservationReadStore
{
    private readonly List<ReservationSummaryDTO> _reservas = [];

    public Task<PagedResult<ReservationSummaryDTO>> ListByRenterAsync(
        Guid renterId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = _reservas.Where(r => r.RenterId == renterId).ToList();
        return Task.FromResult(new PagedResult<ReservationSummaryDTO>(items, page, pageSize, items.Count));
    }

    public Task<PagedResult<ReservationSummaryDTO>> ListByHostAsync(
        Guid hostId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<PagedResult<ReservationSummaryDTO>> ListAllAsync(
        ReservationStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _reservas.AsQueryable();
        if (status is not null)
            query = query.Where(r => r.Status == status);
        var items = query.ToList();
        return Task.FromResult(new PagedResult<ReservationSummaryDTO>(items, page, pageSize, items.Count));
    }

    public void Agregar(ReservationSummaryDTO reserva) => _reservas.Add(reserva);
}
