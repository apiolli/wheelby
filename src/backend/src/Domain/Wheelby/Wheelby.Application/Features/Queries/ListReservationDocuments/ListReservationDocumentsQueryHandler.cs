using AccessControl.Contracts;
using Documents.Contracts;
using MediatR;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.ListReservationDocuments;

internal sealed class ListReservationDocumentsQueryHandler(
    IReservationReadStore reservations,
    IDocumentAccess documents,
    ICurrentUser currentUser)
    : IRequestHandler<ListReservationDocumentsQuery, IReadOnlyList<ReservationDocumentDTO>>
{
    public async Task<IReadOnlyList<ReservationDocumentDTO>> Handle(
        ListReservationDocumentsQuery query,
        CancellationToken cancellationToken)
    {
        await ReservationAccess.GetVisibleAsync(reservations, currentUser, query.ReservationId, cancellationToken);

        var attached = await reservations.ListDocumentsAsync(query.ReservationId, cancellationToken);

        // A lo sumo ReservationDocument.MaxPerReservation consultas: se completan nombre y tipo.
        var result = new List<ReservationDocumentDTO>(attached.Count);
        foreach (var item in attached)
        {
            var info = await documents.GetInfoAsync(item.DocumentId, cancellationToken);
            result.Add(info is null
                ? item
                : item with { FileName = info.FileName, ContentType = info.ContentType, SizeBytes = info.SizeBytes });
        }

        return result;
    }
}
