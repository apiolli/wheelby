using Documents.Application.Abstractions;

namespace Documents.Infrastructure.Persistence;

internal sealed class DocumentsUnitOfWork(DocumentsDbContext db) : IDocumentsUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
