namespace Documents.Application.Abstractions;

// RF-DOC-05: almacenamiento de los bytes del archivo, fuera de la base de datos.
public interface IFileStorage
{
    Task SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken);

    // Devuelve null si el archivo no existe.
    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken);

    // Solo compensa una subida fallida. Nunca se usa en la eliminación lógica.
    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken);
}
