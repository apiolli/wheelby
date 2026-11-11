using Documents.Domain.Documents;
using Documents.Domain.Documents.Exceptions;
using Documents.Test.TestSupport;

namespace Documents.Test.Domain;

public class DocumentTests
{
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void RF_DOC_01_Crear_con_tamano_cero_es_rechazado()
    {
        // Arrange
        var size = 0L;

        // Act
        var ex = Record.Exception(() => Document.Create(Owner, "a.pdf", "application/pdf", size, Moments.T0));

        // Assert
        Assert.IsType<InvalidDocumentDataException>(ex);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("carpeta/")]
    public void RF_DOC_01_Crear_con_nombre_vacio_es_rechazado(string fileName)
    {
        // Arrange (datos en el InlineData)

        // Act
        var ex = Record.Exception(() => Document.Create(Owner, fileName, "application/pdf", 10, Moments.T0));

        // Assert
        Assert.IsType<InvalidDocumentDataException>(ex);
    }

    [Fact]
    public void RF_DOC_01_Crear_con_nombre_de_mas_de_255_caracteres_es_rechazado()
    {
        // Arrange
        var fileName = new string('a', 252) + ".pdf"; // 256 caracteres

        // Act
        var ex = Record.Exception(() => Document.Create(Owner, fileName, "application/pdf", 10, Moments.T0));

        // Assert
        Assert.IsType<InvalidDocumentDataException>(ex);
    }

    [Fact]
    public void RF_DOC_01_Crear_con_fecha_que_no_esta_en_UTC_es_rechazado()
    {
        // Arrange
        var local = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Local);

        // Act
        var ex = Record.Exception(() => Document.Create(Owner, "a.pdf", "application/pdf", 10, local));

        // Assert
        Assert.IsType<InvalidDocumentDataException>(ex);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void RF_DOC_01_Crear_sin_tipo_usa_application_octet_stream(string? contentType)
    {
        // Arrange (datos en el InlineData)

        // Act
        var document = Document.Create(Owner, "a.bin", contentType, 10, Moments.T0);

        // Assert
        Assert.Equal("application/octet-stream", document.ContentType);
    }

    [Fact]
    public void RF_DOC_05_StoredFileName_no_contiene_el_nombre_original()
    {
        // Arrange
        var fileName = "../../etc/passwd";

        // Act
        var document = Document.Create(Owner, fileName, "text/plain", 10, Moments.T0);

        // Assert
        Assert.Equal(document.Id.ToString("N"), document.StoredFileName);
        Assert.DoesNotContain("passwd", document.StoredFileName);
        Assert.Equal("passwd", document.FileName);
    }

    [Fact]
    public void RF_DOC_01_Crear_quita_la_ruta_y_los_caracteres_de_control_del_nombre()
    {
        // Arrange
        var fileName = "C:\\Users\\ana\\in\u0007forme.pdf  ";

        // Act
        var document = Document.Create(Owner, fileName, "application/pdf", 10, Moments.T0);

        // Assert
        Assert.Equal("informe.pdf", document.FileName);
    }

    [Fact]
    public void RF_DOC_04_MarkDeleted_dos_veces_lanza_404()
    {
        // Arrange
        var document = Moments.NewDocument(Owner);
        document.MarkDeleted(Owner, Moments.T1);

        // Act
        var ex = Record.Exception(() => document.MarkDeleted(Owner, Moments.T1));

        // Assert
        Assert.IsType<DocumentNotFoundException>(ex);
        Assert.Equal(404, ((DocumentNotFoundException)ex).StatusCode);
    }

    [Fact]
    public void RF_DOC_04_MarkDeleted_registra_fecha_y_quien_elimina()
    {
        // Arrange
        var document = Moments.NewDocument(Owner);
        var admin = Guid.Parse("99999999-9999-9999-9999-999999999999");

        // Act
        document.MarkDeleted(admin, Moments.T1);

        // Assert
        Assert.True(document.IsDeleted);
        Assert.Equal(Moments.T1, document.DeletedAt);
        Assert.Equal(admin, document.DeletedBy);
    }
}
