using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Reservations;

namespace Wheelby.Test.TestSupport;

// Doble de IReservationReadStore para pruebas de listados y detalle.
public sealed class InMemoryReservationReadStore : IReservationReadStore
{
    private readonly List<ReservationSummaryDTO> _reservas = [];

    public Task<ReservationSummaryDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_reservas.FirstOrDefault(r => r.Id == id));

    public Task<PagedResult<ReservationSummaryDTO>> ListByRenterAsync(
        Guid renterId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = _reservas.Where(r => r.RenterId == renterId).ToList();
        return Task.FromResult(new PagedResult<ReservationSummaryDTO>(items, page, pageSize, items.Count));
    }

    public Task<PagedResult<ReservationSummaryDTO>> ListByHostAsync(
        Guid hostId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = _reservas.Where(r => r.OwnerId == hostId).ToList();
        return Task.FromResult(new PagedResult<ReservationSummaryDTO>(items, page, pageSize, items.Count));
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

    public List<ReservationStatusChangeDTO> Historial { get; } = [];
    public List<(Guid ReservationId, ReservationDocumentDTO Documento)> Documentos { get; } = [];
    public List<ReviewDTO> Resenas { get; } = [];

    public Task<IReadOnlyList<ReservationStatusChangeDTO>> ListHistoryAsync(Guid reservationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ReservationStatusChangeDTO>>(Historial);

    public Task<IReadOnlyList<ReservationDocumentDTO>> ListDocumentsAsync(Guid reservationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ReservationDocumentDTO>>(
            Documentos.Where(d => d.ReservationId == reservationId).Select(d => d.Documento).ToList());

    public Task<IReadOnlyList<ReviewDTO>> ListReviewsAsync(Guid reservationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ReviewDTO>>(Resenas.Where(r => r.ReservationId == reservationId).ToList());
}
