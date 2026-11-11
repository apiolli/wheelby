using SharedKernel.Exceptions;

namespace Documents.Domain.Documents.Exceptions;

// Excepción de dominio (400) para datos inválidos de un documento.
public sealed class InvalidDocumentDataException : BadRequestException
{
    public InvalidDocumentDataException(string message) : base(message) { }
}
