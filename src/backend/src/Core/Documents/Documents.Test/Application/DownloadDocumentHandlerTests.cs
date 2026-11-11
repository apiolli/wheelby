using System.Text;
using Documents.Application.Features.DownloadDocument;
using Documents.Domain.Documents;
using Documents.Test.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Exceptions;

namespace Documents.Test.Application;

public class DownloadDocumentHandlerTests
{
    private static readonly Guid Owner = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Other = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Admin = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static (DownloadDocumentQueryHandler handler, Document document) Arrange(
        Guid userId, string role, bool deleted = false, bool withFile = true)
    {
        var document = Moments.NewDocument(Owner, "contrato.pdf");
        if (deleted)
            document.MarkDeleted(Owner, Moments.T1);

        var storage = new InMemoryFileStorage();
        if (withFile)
            storage.Files[document.StoredFileName] = Encoding.UTF8.GetBytes("pdf");

        var handler = new DownloadDocumentQueryHandler(
            new FakeDocumentRepository(document),
            storage,
            new MutableCurrentUser(userId, role),
            NullLogger<DownloadDocumentQueryHandler>.Instance);
        return (handler, document);
    }

    private static async Task<string> ReadAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task RF_DOC_03_El_duenio_descarga_su_documento()
    {
        // Arrange
        var (handler, document) = Arrange(Owner, "Standard");

        // Act
        var result = await handler.Handle(new DownloadDocumentQuery(document.Id), CancellationToken.None);

        // Assert
        Assert.Equal("pdf", await ReadAsync(result.Content));
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("contrato.pdf", result.FileName);
    }

    [Fact]
    public async Task RF_DOC_03_El_Administrador_descarga_uno_ajeno()
    {
        // Arrange
        var (handler, document) = Arrange(Admin, "Administrator");

        // Act
        var result = await handler.Handle(new DownloadDocumentQuery(document.Id), CancellationToken.None);

        // Assert
        Assert.Equal("pdf", await ReadAsync(result.Content));
    }

    [Fact]
    public async Task RF_DOC_03_Otro_Estandar_recibe_403()
    {
        // Arrange
        var (handler, document) = Arrange(Other, "Standard");

        // Act
        var ex = await Record.ExceptionAsync(() => handler.Handle(new DownloadDocumentQuery(document.Id), CancellationToken.None));

        // Assert
        Assert.IsType<ForbiddenException>(ex);
    }

    [Fact]
    public async Task RF_DOC_03_Un_eliminado_da_404()
    {
        // Arrange
        var (handler, document) = Arrange(Owner, "Standard", deleted: true);

        // Act
        var ex = await Record.ExceptionAsync(() => handler.Handle(new DownloadDocumentQuery(document.Id), CancellationToken.None));

        // Assert
        Assert.IsAssignableFrom<NotFoundException>(ex);
    }

    [Fact]
    public async Task RF_DOC_03_Un_id_inexistente_da_404()
    {
        // Arrange
        var (handler, _) = Arrange(Owner, "Standard");

        // Act
        var ex = await Record.ExceptionAsync(() => handler.Handle(new DownloadDocumentQuery(Guid.NewGuid()), CancellationToken.None));

        // Assert
        Assert.IsAssignableFrom<NotFoundException>(ex);
    }

    [Fact]
    public async Task RF_DOC_03_Archivo_ausente_en_disco_da_404()
    {
        // Arrange
        var (handler, document) = Arrange(Owner, "Standard", withFile: false);

        // Act
        var ex = await Record.ExceptionAsync(() => handler.Handle(new DownloadDocumentQuery(document.Id), CancellationToken.None));

        // Assert
        var notFound = Assert.IsAssignableFrom<NotFoundException>(ex);
        Assert.Equal("El archivo del documento no está disponible.", notFound.Message);
    }

    [Fact]
    public async Task RF_DOC_03_Precedencia_ajeno_y_eliminado_da_404_y_no_403()
    {
        // Arrange
        var (handler, document) = Arrange(Other, "Standard", deleted: true);

        // Act
        var ex = await Record.ExceptionAsync(() => handler.Handle(new DownloadDocumentQuery(document.Id), CancellationToken.None));

        // Assert
        Assert.IsAssignableFrom<NotFoundException>(ex);
    }
}
