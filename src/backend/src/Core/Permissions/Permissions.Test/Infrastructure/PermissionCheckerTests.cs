using Microsoft.EntityFrameworkCore;
using Permissions.Contracts;
using Permissions.Domain.PermissionGrants;
using Permissions.Infrastructure.Permissions;
using Permissions.Test.Support;

namespace Permissions.Test.Infrastructure;

// RF-GP-05: la comprobación del permiso consulta PermissionGrants con EF;
// se prueba sobre PermissionsDbContext con EF InMemory.
public sealed class PermissionCheckerTests
{
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtroUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task RF_GP_05_Checker_devuelve_true_solo_para_usuario_y_codigo()
    {
        // Arrange
        using var db = PermissionsDb.CreaContexto();
        db.Set<PermissionGrant>().Add(
            PermissionGrant.Create(UsuarioId, "core.demo-action", Guid.NewGuid(), Ahora));
        await db.SaveChangesAsync();
        var checker = new PermissionChecker(db);

        // Act
        var verdad = await checker.HasPermissionAsync(UsuarioId, "core.demo-action");
        var otroUsuario = await checker.HasPermissionAsync(OtroUserId, "core.demo-action");
        var otroCodigo = await checker.HasPermissionAsync(UsuarioId, "core.otro-codigo");

        // Assert
        Assert.True(verdad);
        Assert.False(otroUsuario);
        Assert.False(otroCodigo);
    }
}
