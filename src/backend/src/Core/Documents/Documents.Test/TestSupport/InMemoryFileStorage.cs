using Documents.Application.Abstractions;

namespace Documents.Test.TestSupport;

// Almacenamiento falso en memoria. Registra las llamadas a DeleteAsync.
public sealed class InMemoryFileStorage : IFileStorage
{
    public Dictionary<string, byte[]> Files { get; } = new();
    public List<string> DeletedNames { get; } = new();

    public async Task SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        Files[storedFileName] = buffer.ToArray();
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
        => Task.FromResult<Stream?>(
            Files.TryGetValue(storedFileName, out var bytes) ? new MemoryStream(bytes) : null);

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        DeletedNames.Add(storedFileName);
        Files.Remove(storedFileName);
        return Task.CompletedTask;
    }
}
