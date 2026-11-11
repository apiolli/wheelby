namespace Documents.Application.DTOs;

// RF-DOC-01: nombre, tipo, tamaño, fecha y quién lo subió.
public sealed record DocumentSummaryDTO(
    Guid Id,
    Guid OwnerId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt);
