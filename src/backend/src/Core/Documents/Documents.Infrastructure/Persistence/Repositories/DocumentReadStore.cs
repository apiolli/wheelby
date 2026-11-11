using Documents.Application.Abstractions;
using Documents.Application.Common;
using Documents.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Documents.Infrastructure.Persistence;

internal sealed class DocumentReadStore(DocumentsDbContext db) : IDocumentReadStore
{
    public async Task<PagedResult<DocumentSummaryDTO>> ListAsync(
        Guid? ownerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // RF-DOC-02 y 04: los eliminados nunca se listan.
        var query = db.Documents
            .AsNoTracking()
            .Where(d => d.DeletedAt == null);

        if (ownerId is not null)
            query = query.Where(d => d.OwnerId == ownerId.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.UploadedAt).ThenBy(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentSummaryDTO(
                d.Id, d.OwnerId, d.FileName, d.ContentType, d.SizeBytes, d.UploadedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<DocumentSummaryDTO>(items, page, pageSize, total);
    }
}
