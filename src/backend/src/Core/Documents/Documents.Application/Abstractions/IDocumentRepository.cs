using Documents.Domain.Documents;

namespace Documents.Application.Abstractions;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken);

    // También devuelve los eliminados: el handler decide qué hacer con ellos.
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
