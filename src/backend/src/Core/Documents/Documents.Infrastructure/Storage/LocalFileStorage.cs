using Documents.Application.Abstractions;
using Documents.Application.Common;
using Microsoft.Extensions.Options;

namespace Documents.Infrastructure.Storage;

// RF-DOC-05: guarda los archivos en una carpeta local (en el contenedor, un volumen con nombre).
internal sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<DocumentsOptions> options)
    {
        var configured = options.Value.StoragePath;
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "data", "documents")
            : configured;

        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken)
    {
        var finalPath = ResolvePath(storedFileName);

        // Se escribe en un temporal de la misma carpeta y se mueve al final:
        // nunca queda un archivo a medias con el nombre definitivo.
        var tempPath = Path.Combine(_root, $"{storedFileName}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var file = new FileStream(
                tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true))
            {
                await content.CopyToAsync(file, cancellationToken);
            }

            File.Move(tempPath, finalPath, overwrite: false);
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
    {
        var path = ResolvePath(storedFileName);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        var path = ResolvePath(storedFileName);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    // Solo acepta un GUID en formato N: así no hay recorrido de rutas aunque alguien llame mal.
    private string ResolvePath(string storedFileName)
    {
        if (!Guid.TryParseExact(storedFileName, "N", out _))
            throw new ArgumentException(
                "El nombre almacenado debe ser un GUID en formato N.", nameof(storedFileName));

        return Path.Combine(_root, storedFileName);
    }
}
