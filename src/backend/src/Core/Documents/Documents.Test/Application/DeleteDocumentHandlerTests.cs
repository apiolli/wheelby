using Documents.Application.Features.DeleteDocument;
using Documents.Domain.Documents;
using Documents.Test.TestSupport;
using SharedKernel.Exceptions;

namespace Documents.Test.Application;

public class DeleteDocumentHandlerTests
{
    private static readonly Guid Owner = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Other = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private sealed record Setup(
        DeleteDocumentCommandHandler Handler,
        Document Document,
        FakeDocumentRepository Repository,
        InMemoryFileStorage Storage,
        RecordingUnitOfWork UnitOfWork);

    private static Setup Arrange(Guid userId, string role = "Standard", bool deleted = false)
    {
        var document = Moments.NewDocument(Owner);
        if (deleted)
            document.MarkDeleted(Owner, Moments.T0);

        var repository = new FakeDocumentRepository(document);
        var storage = new InMemoryFileStorage();
        storage.Files[document.StoredFileName] = [1, 2, 3];
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new DeleteDocumentCommandHandler(
            repository, unitOfWork, new MutableCurrentUser(userId, role), new FixedClock(Moments.T1));
        return new Setup(handler, document, repository, storage, unitOfWork);
    }

    [Fact]
    public async Task RF_DOC_04_Eliminar_deja_la_fila_con_DeletedAt_y_DeletedBy()
    {
        // Arrange
        var s = Arrange(Owner);

        // Act
        await s.Handler.Handle(new DeleteDocumentCommand(s.Document.Id), CancellationToken.None);

        // Assert
        var row = s.Repository.Documents[s.Document.Id];
        Assert.Equal(new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc), row.DeletedAt);
        Assert.Equal(Owner, row.DeletedBy);
        Assert.Equal(1, s.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_DOC_04_Eliminar_no_borra_el_archivo()
    {
        // Arrange
        var s = Arrange(Owner);

        // Act
        await s.Handler.Handle(new DeleteDocumentCommand(s.Document.Id), CancellationToken.None);

        // Assert
        Assert.Empty(s.Storage.DeletedNames);
        Assert.True(s.Storage.Files.ContainsKey(s.Document.StoredFileName));
    }

    [Fact]
    public async Task RF_DOC_04_Otro_Estandar_recibe_403_y_el_documento_queda_intacto()
    {
        // Arrange
        var s = Arrange(Other);

        // Act
        var ex = await Record.ExceptionAsync(() => s.Handler.Handle(new DeleteDocumentCommand(s.Document.Id), CancellationToken.None));

        // Assert
        Assert.IsType<ForbiddenException>(ex);
        Assert.Null(s.Document.DeletedAt);
        Assert.Null(s.Document.DeletedBy);
        Assert.Equal(0, s.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_DOC_04_El_Administrador_elimina_uno_ajeno()
    {
        // Arrange
        var admin = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var s = Arrange(admin, "Administrator");

        // Act
        await s.Handler.Handle(new DeleteDocumentCommand(s.Document.Id), CancellationToken.None);

        // Assert
        Assert.Equal(admin, s.Document.DeletedBy);
    }

    [Fact]
    public async Task RF_DOC_04_Eliminar_uno_ya_eliminado_da_404()
    {
        // Arrange
        var s = Arrange(Owner, deleted: true);

        // Act
        var ex = await Record.ExceptionAsync(() => s.Handler.Handle(new DeleteDocumentCommand(s.Document.Id), CancellationToken.None));

        // Assert
        Assert.IsAssignableFrom<NotFoundException>(ex);
        Assert.Equal(0, s.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_DOC_04_Precedencia_ajeno_y_eliminado_da_404_y_no_403()
    {
        // Arrange
        var s = Arrange(Other, deleted: true);

        // Act
        var ex = await Record.ExceptionAsync(() => s.Handler.Handle(new DeleteDocumentCommand(s.Document.Id), CancellationToken.None));

        // Assert
        Assert.IsAssignableFrom<NotFoundException>(ex);
    }
}
