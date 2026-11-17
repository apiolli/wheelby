using Shared.Application.Abstractions;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.ListReservationDocuments;

public sealed record ListReservationDocumentsQuery(Guid ReservationId) : IQuery<IReadOnlyList<ReservationDocumentDTO>>;
