using Documents.Domain.Documents;

namespace Documents.Test.TestSupport;

// Fechas fijas y fábrica de documentos para el Arrange.
internal static class Moments
{
    public static readonly DateTime T0 = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime T1 = new(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);

    public static Document NewDocument(Guid ownerId, string fileName = "contrato.pdf", DateTime? at = null)
        => Document.Create(ownerId, fileName, "application/pdf", 1234, at ?? T0);
}
