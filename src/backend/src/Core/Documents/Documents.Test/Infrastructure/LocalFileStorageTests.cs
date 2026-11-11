using Documents.Application.Common;
using Documents.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Documents.Test.Infrastructure;

// RF-DOC-05: LocalFileStorage sobre una carpeta temporal propia de cada prueba.
public class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"documents-test-{Guid.NewGuid():N}");

    private LocalFileStorage CreateStorage()
        => new(Options.Create(new DocumentsOptions { StoragePath = _root }));

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void RF_DOC_05_Crea_la_carpeta_al_construirse()
    {
        // Arrange (carpeta temporal en _root)

        // Act
        CreateStorage();

        // Assert
        Assert.True(Directory.Exists(_root));
    }

    [Fact]
    public async Task RF_DOC_05_Guardar_y_leer_devuelve_los_mismos_bytes()
    {
        // Arrange
        var storage = CreateStorage();
        var name = "0123456789abcdef0123456789abcdef";
        var bytes = new byte[] { 1, 2, 3, 250, 0, 7 };

        // Act
        await storage.SaveAsync(name, new MemoryStream(bytes), CancellationToken.None);
        await using var read = await storage.OpenReadAsync(name, CancellationToken.None);

        // Assert
        Assert.NotNull(read);
        using var copy = new MemoryStream();
        await read!.CopyToAsync(copy);
        Assert.Equal(bytes, copy.ToArray());
    }

    [Fact]
    public async Task RF_DOC_05_Guardar_no_deja_temporales_en_la_carpeta()
    {
        // Arrange
        var storage = CreateStorage();
        var name = "0123456789abcdef0123456789abcdef";

        // Act
        await storage.SaveAsync(name, new MemoryStream([1, 2]), CancellationToken.None);

        // Assert
        var files = Directory.GetFiles(_root).Select(Path.GetFileName).ToList();
        Assert.Equal(new[] { name }, files);
    }

    [Fact]
    public async Task RF_DOC_05_Leer_uno_inexistente_devuelve_null()
    {
        // Arrange
        var storage = CreateStorage();

        // Act
        var read = await storage.OpenReadAsync("ffffffffffffffffffffffffffffffff", CancellationToken.None);

        // Assert
        Assert.Null(read);
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("contrato.pdf")]
    [InlineData("0123456789abcdef0123456789abcde/")]
    public async Task RF_DOC_05_Un_nombre_que_no_es_GUID_se_rechaza(string name)
    {
        // Arrange
        var storage = CreateStorage();

        // Act
        var ex = await Record.ExceptionAsync(() => storage.SaveAsync(name, new MemoryStream([1]), CancellationToken.None));

        // Assert
        Assert.IsType<ArgumentException>(ex);
    }
}
