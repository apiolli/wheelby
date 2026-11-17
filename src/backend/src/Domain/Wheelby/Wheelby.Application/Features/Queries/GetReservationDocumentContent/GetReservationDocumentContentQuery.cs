using Shared.Application.Abstractions;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.GetReservationDocumentContent;

public sealed record GetReservationDocumentContentQuery(Guid ReservationId, Guid DocumentId) : IQuery<FileContentDTO>;
