using Documents.Application.Features.ListDocuments;
using Documents.Infrastructure.Persistence;
using Documents.Test.TestSupport;

namespace Documents.Test.Application;

// RF-DOC-02 con el DocumentReadStore real sobre EF InMemory.
public class ListDocumentsHandlerTests
{
    private static readonly Guid Ana = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Luis = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Admin = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    // Ana: un vigente y uno eliminado. Luis: uno vigente.
    private static async Task<(DocumentsDbContext db, Guid anaVigente, Guid anaEliminado, Guid luisVigente)> SeedAsync()
    {
        var db = DocumentsDb.Create();
        var anaVigente = Moments.NewDocument(Ana, "ana.pdf", Moments.T0);
        var anaEliminado = Moments.NewDocument(Ana, "ana-borrado.pdf", Moments.T0);
        anaEliminado.MarkDeleted(Ana, Moments.T1);
        var luisVigente = Moments.NewDocument(Luis, "luis.pdf", Moments.T1);
        db.Documents.AddRange(anaVigente, anaEliminado, luisVigente);
        await db.SaveChangesAsync();
        return (db, anaVigente.Id, anaEliminado.Id, luisVigente.Id);
    }

    [Fact]
    public async Task RF_DOC_02_Un_Estandar_no_ve_eliminados_ni_ajenos()
    {
        // Arrange
        var (db, anaVigente, _, _) = await SeedAsync();
        var handler = new ListDocumentsQueryHandler(new DocumentReadStore(db), new MutableCurrentUser(Ana, "Standard"));

        // Act
        var page = await handler.Handle(new ListDocumentsQuery(1, 20), CancellationToken.None);

        // Assert
        var item = Assert.Single(page.Items);
        Assert.Equal(anaVigente, item.Id);
        Assert.Equal(1, page.TotalItems);
    }

    [Fact]
    public async Task RF_DOC_02_El_Administrador_ve_los_de_todos_sin_los_eliminados()
    {
        // Arrange
        var (db, anaVigente, anaEliminado, luisVigente) = await SeedAsync();
        var handler = new ListDocumentsQueryHandler(new DocumentReadStore(db), new MutableCurrentUser(Admin, "Administrator"));

        // Act
        var page = await handler.Handle(new ListDocumentsQuery(1, 20), CancellationToken.None);

        // Assert
        var ids = page.Items.Select(i => i.Id).ToList();
        Assert.Equal(new[] { luisVigente, anaVigente }, ids); // UploadedAt descendente
        Assert.DoesNotContain(anaEliminado, ids);
    }

    [Fact]
    public async Task RF_DOC_02_El_resumen_trae_nombre_tipo_tamano_fecha_y_duenio()
    {
        // Arrange
        var (db, _, _, _) = await SeedAsync();
        var handler = new ListDocumentsQueryHandler(new DocumentReadStore(db), new MutableCurrentUser(Luis, "Standard"));

        // Act
        var page = await handler.Handle(new ListDocumentsQuery(1, 20), CancellationToken.None);

        // Assert
        var item = Assert.Single(page.Items);
        Assert.Equal(Luis, item.OwnerId);
        Assert.Equal("luis.pdf", item.FileName);
        Assert.Equal("application/pdf", item.ContentType);
        Assert.Equal(1234, item.SizeBytes);
        Assert.Equal(new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc), item.UploadedAt);
    }
}
