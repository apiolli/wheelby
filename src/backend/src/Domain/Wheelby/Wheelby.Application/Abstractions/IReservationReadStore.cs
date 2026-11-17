using Wheelby.Application.Common;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Reservations;

namespace Wheelby.Application.Abstractions;

// Puerto de lectura de reservas.
public interface IReservationReadStore
{
    Task<ReservationSummaryDTO?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationStatusChangeDTO>> ListHistoryAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationDocumentDTO>> ListDocumentsAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReviewDTO>> ListReviewsAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationSummaryDTO>> ListByRenterAsync(
        Guid renterId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationSummaryDTO>> ListByHostAsync(
        Guid hostId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationSummaryDTO>> ListAllAsync(
        ReservationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
