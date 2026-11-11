using Documents.Application.Common;
using Documents.Application.DTOs;

namespace Documents.Application.Abstractions;

public interface IDocumentReadStore
{
    // Siempre excluye los eliminados. Con ownerId null lista los de todos (solo Administrador).
    // Orden: UploadedAt descendente.
    Task<PagedResult<DocumentSummaryDTO>> ListAsync(
        Guid? ownerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
