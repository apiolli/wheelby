namespace Documents.Application.Abstractions;

public interface IDocumentsUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
