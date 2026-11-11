using Documents.Application.Abstractions;
using Documents.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace Documents.Infrastructure.Persistence;

internal sealed class DocumentRepository(DocumentsDbContext db) : IDocumentRepository
{
    public async Task AddAsync(Document document, CancellationToken cancellationToken)
        => await db.Documents.AddAsync(document, cancellationToken);

    // También devuelve los eliminados: el handler decide (404).
    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
}
