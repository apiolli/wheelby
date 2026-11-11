using SharedKernel.Exceptions;

namespace Documents.Domain.Documents.Exceptions;

// 404: el documento no existe o ya fue eliminado. Para el usuario, un documento
// eliminado ya no existe.
public sealed class DocumentNotFoundException : NotFoundException
{
    public DocumentNotFoundException() : base("El documento no existe.") { }
}
