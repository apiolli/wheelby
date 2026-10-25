using Permissions.Domain.PermissionRequests;
using Permissions.Infrastructure.Persistence;
using Permissions.Test.Support;

namespace Permissions.Test.Infrastructure;

// RF-GP-02: el filtrado por solicitante vive en PermissionRequestReadStore,
// que consulta con EF: se prueba sobre PermissionsDbContext con EF InMemory.
public sealed class PermissionRequestReadStoreTests
{
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UsuarioA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UsuarioB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AdministradorId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task RF_GP_02_Las_solicitudes_de_una_persona_excluyen_a_los_demas()
    {
        // Arrange
        var pendienteDeA = PermissionRequest.Create(UsuarioA, "core.demo-action", Ahora);
        var rechazadaDeB = PermissionRequest.Create(UsuarioB, "core.demo-action", Ahora);
        rechazadaDeB.Reject(AdministradorId, Ahora.AddMinutes(5), "No corresponde");
        var aplicadaDeB = PermissionRequest.Create(UsuarioB, "core.otro-codigo", Ahora);
        aplicadaDeB.Approve(AdministradorId, Ahora.AddMinutes(5));
        aplicadaDeB.MarkApplied(Ahora.AddMinutes(5));

        using var db = PermissionsDb.CreaContexto();
        db.Set<PermissionRequest>().AddRange(pendienteDeA, rechazadaDeB, aplicadaDeB);
        await db.SaveChangesAsync();
        var store = new PermissionRequestReadStore(db);

        // Act
        var propia = await store.ListByRequesterAsync(UsuarioA, 1, 20, default);
        var ajena = await store.ListByRequesterAsync(UsuarioB, 1, 20, default);

        // Assert
        Assert.Equal(1, propia.TotalItems);
        var propiaItem = Assert.Single(propia.Items);
        Assert.Equal(UsuarioA, propiaItem.RequesterId);
        Assert.Equal(pendienteDeA.Id, propiaItem.Id);
        Assert.All(ajena.Items, item => Assert.Equal(UsuarioB, item.RequesterId));
        Assert.Equal(2, ajena.TotalItems);
    }

    [Fact]
    public async Task RF_GP_02_Las_pendientes_excluyen_las_resueltas()
    {
        // Arrange
        var pendiente = PermissionRequest.Create(UsuarioA, "core.demo-action", Ahora);
        var aprobada = PermissionRequest.Create(UsuarioA, "core.otro-codigo", Ahora);
        aprobada.Approve(AdministradorId, Ahora.AddMinutes(5));
        var aplicada = PermissionRequest.Create(UsuarioB, "core.demo-action", Ahora);
        aplicada.Approve(AdministradorId, Ahora.AddMinutes(5));
        aplicada.MarkApplied(Ahora.AddMinutes(5));
        var rechazada = PermissionRequest.Create(UsuarioB, "core.otro-codigo", Ahora);
        rechazada.Reject(AdministradorId, Ahora.AddMinutes(5), "No corresponde");

        using var db = PermissionsDb.CreaContexto();
        db.Set<PermissionRequest>().AddRange(pendiente, aprobada, aplicada, rechazada);
        await db.SaveChangesAsync();
        var store = new PermissionRequestReadStore(db);

        // Act
        var pagina = await store.ListPendingAsync(1, 20, default);

        // Assert
        Assert.Equal(1, pagina.TotalItems);
        var item = Assert.Single(pagina.Items);
        Assert.Equal(PermissionRequestStatus.Pending.ToString(), item.Status);
        Assert.Equal(pendiente.Id, item.Id);
    }
}
