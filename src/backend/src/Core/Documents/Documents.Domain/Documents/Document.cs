using SharedKernel.Domain;
using Documents.Domain.Documents.Exceptions;

namespace Documents.Domain.Documents;

// Documento subido por un usuario. Raíz de agregado; constructor privado para EF.
public sealed class Document : AggregateRoot<Guid>
{
    public const int FileNameMaxLength = 255;
    public const int ContentTypeMaxLength = 100;
    public const int StoredFileNameLength = 32;
    public const string DefaultContentType = "application/octet-stream";

    public Guid OwnerId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StoredFileName { get; private set; } = string.Empty;
    public DateTime UploadedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    private Document() : base(Guid.Empty) { }

    private Document(
        Guid id,
        Guid ownerId,
        string fileName,
        string contentType,
        long sizeBytes,
        DateTime utcNow)
        : base(id)
    {
        OwnerId = ownerId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        // El nombre en disco sale del Id, nunca del nombre que envía el usuario.
        StoredFileName = id.ToString("N");
        UploadedAt = utcNow;
    }

    public static Document Create(
        Guid ownerId,
        string fileName,
        string? contentType,
        long sizeBytes,
        DateTime utcNow)
    {
        if (ownerId == Guid.Empty)
            throw new InvalidDocumentDataException("El propietario es obligatorio.");

        var safeName = SanitizeFileName(fileName);
        if (safeName.Length == 0)
            throw new InvalidDocumentDataException("El nombre del archivo es obligatorio.");

        if (safeName.Length > FileNameMaxLength)
            throw new InvalidDocumentDataException(
                $"El nombre del archivo no puede exceder los {FileNameMaxLength} caracteres.");

        var type = string.IsNullOrWhiteSpace(contentType) ? DefaultContentType : contentType.Trim();
        if (type.Length > ContentTypeMaxLength)
            throw new InvalidDocumentDataException(
                $"El tipo de contenido no puede exceder los {ContentTypeMaxLength} caracteres.");

        if (sizeBytes <= 0)
            throw new InvalidDocumentDataException("El archivo está vacío.");

        if (utcNow.Kind != DateTimeKind.Utc)
            throw new InvalidDocumentDataException("La fecha debe estar en UTC.");

        return new Document(Guid.NewGuid(), ownerId, safeName, type, sizeBytes, utcNow);
    }

    // RF-DOC-04: eliminación lógica. La fila y el archivo se conservan.
    public void MarkDeleted(Guid deletedBy, DateTime utcNow)
    {
        if (IsDeleted)
            throw new DocumentNotFoundException();

        if (deletedBy == Guid.Empty)
            throw new InvalidDocumentDataException("Quién elimina el documento es obligatorio.");

        if (utcNow.Kind != DateTimeKind.Utc)
            throw new InvalidDocumentDataException("La fecha debe estar en UTC.");

        DeletedAt = utcNow;
        DeletedBy = deletedBy;
    }

    // RF-DOC-03 y 04: única regla de acceso. Los handlers la usan y no la repiten.
    public bool CanBeAccessedBy(Guid userId, bool isAdministrator)
        => isAdministrator || userId == OwnerId;

    // Solo conserva el nombre final (sin carpetas), sin caracteres de control y recortado.
    private static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        // Algunos navegadores envían la ruta completa de Windows; se normaliza antes de cortar.
        var name = Path.GetFileName(fileName.Replace('\\', '/'));
        var withoutControl = new string(name.Where(c => !char.IsControl(c)).ToArray());
        return withoutControl.Trim();
    }
}
