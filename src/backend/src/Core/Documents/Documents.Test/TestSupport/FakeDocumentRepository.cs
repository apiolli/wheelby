using Documents.Application.Abstractions;
using Documents.Domain.Documents;

namespace Documents.Test.TestSupport;

// Repositorio en memoria. Devuelve también los eliminados, como el real.
public sealed class FakeDocumentRepository : IDocumentRepository
{
    public Dictionary<Guid, Document> Documents { get; } = new();

    public FakeDocumentRepository(params Document[] seed)
    {
        foreach (var document in seed)
            Documents[document.Id] = document;
    }

    public Task AddAsync(Document document, CancellationToken cancellationToken)
    {
        Documents[document.Id] = document;
        return Task.CompletedTask;
    }

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Documents.TryGetValue(id, out var document) ? document : null);
}
