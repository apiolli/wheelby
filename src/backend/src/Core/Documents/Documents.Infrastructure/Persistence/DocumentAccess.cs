using Documents.Application.Abstractions;
using Documents.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Documents.Infrastructure.Persistence;

internal sealed class DocumentAccess(
    DocumentsDbContext db,
    IFileStorage storage,
    ILogger<DocumentAccess> logger) : IDocumentAccess
{
    public Task<DocumentInfo?> GetInfoAsync(Guid documentId, CancellationToken cancellationToken)
        => db.Documents
            .AsNoTracking()
            .Where(d => d.Id == documentId && d.DeletedAt == null)
            .Select(d => new DocumentInfo(d.Id, d.OwnerId, d.FileName, d.ContentType, d.SizeBytes))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<DocumentFile?> OpenReadAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await db.Documents
            .AsNoTracking()
            .Where(d => d.Id == documentId && d.DeletedAt == null)
            .Select(d => new { d.Id, d.StoredFileName, d.ContentType, d.FileName })
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
            return null;

        var content = await storage.OpenReadAsync(document.StoredFileName, cancellationToken);
        if (content is null)
        {
            logger.LogError("El archivo del documento {DocumentId} no está en el almacenamiento.", document.Id);
            return null;
        }

        return new DocumentFile(content, document.ContentType, document.FileName);
    }
}
