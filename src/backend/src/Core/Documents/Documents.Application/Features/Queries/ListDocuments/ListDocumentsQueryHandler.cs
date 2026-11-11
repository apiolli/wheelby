using AccessControl.Contracts;
using Documents.Application.Abstractions;
using Documents.Application.Common;
using Documents.Application.DTOs;
using MediatR;

namespace Documents.Application.Features.ListDocuments;

// RF-DOC-02: el Administrador ve los de todos; un Estándar solo los suyos.
internal sealed class ListDocumentsQueryHandler(
    IDocumentReadStore reader,
    ICurrentUser currentUser)
    : IRequestHandler<ListDocumentsQuery, PagedResult<DocumentSummaryDTO>>
{
    public Task<PagedResult<DocumentSummaryDTO>> Handle(
        ListDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        Guid? ownerId = currentUser.Role == AccessControlRoles.Administrator ? null : currentUser.Id;
        return reader.ListAsync(ownerId, request.Page, request.PageSize, cancellationToken);
    }
}
