using System.Text;
using Documents.Application.Common;
using Documents.Application.Features.UploadDocument;
using Documents.Test.TestSupport;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Documents.Test.Application;

public class UploadDocumentHandlerTests
{
    private static readonly Guid User = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static UploadDocumentCommand Command(string text = "hola", string fileName = "nota.txt")
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return new UploadDocumentCommand(fileName, "text/plain", bytes.Length, new MemoryStream(bytes));
    }

    [Fact]
    public async Task RF_DOC_01_Subir_guarda_los_seis_datos_y_el_archivo()
    {
        // Arrange
        var repository = new FakeDocumentRepository();
        var storage = new InMemoryFileStorage();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UploadDocumentCommandHandler(
            repository, storage, unitOfWork, new MutableCurrentUser(User), new FixedClock(Moments.T0));

        // Act
        var id = await handler.Handle(Command("hola"), CancellationToken.None);

        // Assert
        var saved = repository.Documents[id];
        Assert.Equal(User, saved.OwnerId);
        Assert.Equal("nota.txt", saved.FileName);
        Assert.Equal("text/plain", saved.ContentType);
        Assert.Equal(4, saved.SizeBytes);
        Assert.Equal(new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc), saved.UploadedAt);
        Assert.Null(saved.DeletedAt);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal("hola", Encoding.UTF8.GetString(storage.Files[saved.StoredFileName]));
    }

    [Fact]
    public async Task RD_07_Si_falla_SaveChanges_el_archivo_se_borra()
    {
        // Arrange
        var storage = new InMemoryFileStorage();
        var handler = new UploadDocumentCommandHandler(
            new FakeDocumentRepository(), storage, new RecordingUnitOfWork(fail: true),
            new MutableCurrentUser(User), new FixedClock(Moments.T0));

        // Act
        var ex = await Record.ExceptionAsync(() => handler.Handle(Command(), CancellationToken.None));

        // Assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Single(storage.DeletedNames);
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task RD_07_Archivo_vacio_es_rechazado_por_el_validador()
    {
        // Arrange
        var validator = new UploadDocumentCommandValidator(Options.Create(new DocumentsOptions()));
        var command = new UploadDocumentCommand("vacio.txt", "text/plain", 0, new MemoryStream());

        // Act
        var ex = await Record.ExceptionAsync(() => validator.ValidateAndThrowAsync(command));

        // Assert
        var validation = Assert.IsType<ValidationException>(ex);
        Assert.Contains(validation.Errors, e => e.ErrorMessage == "El archivo está vacío.");
    }

    [Fact]
    public async Task RD_07_Archivo_mayor_que_el_limite_es_rechazado_por_el_validador()
    {
        // Arrange
        var options = Options.Create(new DocumentsOptions { MaxFileSizeBytes = 1000 });
        var validator = new UploadDocumentCommandValidator(options);
        var command = new UploadDocumentCommand("grande.bin", "application/octet-stream", 1001, new MemoryStream());

        // Act
        var ex = await Record.ExceptionAsync(() => validator.ValidateAndThrowAsync(command));

        // Assert
        Assert.IsType<ValidationException>(ex);
    }

    [Fact]
    public async Task RD_07_Archivo_justo_en_el_limite_es_aceptado_por_el_validador()
    {
        // Arrange
        var options = Options.Create(new DocumentsOptions { MaxFileSizeBytes = 1000 });
        var validator = new UploadDocumentCommandValidator(options);
        var command = new UploadDocumentCommand("justo.bin", "application/octet-stream", 1000, new MemoryStream());

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RD_07_Limite_por_defecto_es_10_MB()
    {
        // Arrange
        var options = new DocumentsOptions();

        // Act
        var max = options.MaxFileSizeBytes;

        // Assert
        Assert.Equal(10485760, max);
    }
}
