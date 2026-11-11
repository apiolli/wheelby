namespace Documents.Application.Common;

// Configuración del Manejador de documentos (sección "Documents").
public sealed class DocumentsOptions
{
    public const string SectionName = "Documents";
    public const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024;

    public long MaxFileSizeBytes { get; set; } = DefaultMaxFileSizeBytes;
    public string? StoragePath { get; set; }
}
