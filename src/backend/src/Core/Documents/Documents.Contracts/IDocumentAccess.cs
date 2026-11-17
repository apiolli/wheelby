namespace Documents.Contracts;

// Acceso de solo lectura a documentos para otros módulos (RD-03: Documents no
// conoce a quien lo usa; el módulo que llama guarda solo el DocumentId).
// No aplica Document.CanBeAccessedBy: el módulo que llama decide quién puede
// ver el archivo según sus propias reglas (p. ej. participantes de una reserva).
public interface IDocumentAccess
{
    // null si no existe o está eliminado.
    Task<DocumentInfo?> GetInfoAsync(Guid documentId, CancellationToken cancellationToken = default);

    // null si no existe, está eliminado o falta el archivo en el almacenamiento.
    Task<DocumentFile?> OpenReadAsync(Guid documentId, CancellationToken cancellationToken = default);
}

public sealed record DocumentInfo(Guid Id, Guid OwnerId, string FileName, string ContentType, long SizeBytes);

public sealed record DocumentFile(Stream Content, string ContentType, string FileName);
