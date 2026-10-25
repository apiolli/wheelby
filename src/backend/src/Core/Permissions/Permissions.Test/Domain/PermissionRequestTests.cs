using Permissions.Domain.PermissionRequests;
using Permissions.Domain.PermissionRequests.Exceptions;

namespace Permissions.Test.Domain;

public sealed class PermissionRequestTests
{
    private static readonly DateTime Now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Later = Now.AddMinutes(5);
    private static readonly Guid RequesterId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdministratorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void RF_GP_01_Crear_solicitud_nace_en_Pending()
    {
        // Arrange
        var permissionCode = "core.demo-action";

        // Act
        var request = PermissionRequest.Create(RequesterId, permissionCode, Now);

        // Assert
        Assert.Equal(PermissionRequestStatus.Pending, request.Status);
        Assert.Equal(RequesterId, request.RequesterId);
        Assert.Equal(permissionCode, request.PermissionCode);
        Assert.Equal(Now, request.CreatedAt);
        Assert.Null(request.ApproverId);
        Assert.Null(request.ResolvedAt);
        Assert.Null(request.RejectionReason);
    }

    [Fact]
    public void RF_GP_03_Aprobar_registra_el_aprobador_y_la_fecha_del_reloj()
    {
        // Arrange
        var request = PermissionRequest.Create(RequesterId, "core.demo-action", Now);

        // Act
        request.Approve(AdministratorId, Later);

        // Assert
        Assert.Equal(PermissionRequestStatus.Approved, request.Status);
        Assert.Equal(AdministratorId, request.ApproverId);
        Assert.Equal(Later, request.ResolvedAt);
        Assert.Null(request.RejectionReason);
    }

    [Fact]
    public void RF_GP_06_Aprobar_dos_veces_es_rechazado_y_el_estado_no_cambia()
    {
        // Arrange
        var request = PermissionRequest.Create(RequesterId, "core.demo-action", Now);
        request.Approve(AdministratorId, Now);

        // Act
        var exception = Record.Exception(() => request.Approve(AdministratorId, Now));

        // Assert
        Assert.IsType<InvalidPermissionRequestTransitionException>(exception);
        Assert.Equal(PermissionRequestStatus.Approved, request.Status);
        Assert.Equal(AdministratorId, request.ApproverId);
        Assert.Equal(Now, request.ResolvedAt);
        Assert.Null(request.RejectionReason);
    }

    [Fact]
    public void RF_GP_05_Aplicar_aprobada_deja_Applied()
    {
        // Arrange
        var request = PermissionRequest.Create(RequesterId, "core.demo-action", Now);
        request.Approve(AdministratorId, Now);

        // Act
        request.MarkApplied(Now);

        // Assert
        Assert.Equal(PermissionRequestStatus.Applied, request.Status);
        Assert.Equal(AdministratorId, request.ApproverId);
        Assert.Equal(Now, request.ResolvedAt);
        Assert.Null(request.RejectionReason);
    }

    [Fact]
    public void RF_GP_07_Aprobar_la_propia_solicitud_es_rechazado()
    {
        // Arrange
        var request = PermissionRequest.Create(RequesterId, "core.demo-action", Now);

        // Act
        var exception = Record.Exception(() => request.Approve(RequesterId, Now));

        // Assert
        Assert.IsType<CannotResolveOwnPermissionRequestException>(exception);
        Assert.Equal(PermissionRequestStatus.Pending, request.Status);
        Assert.Null(request.ApproverId);
        Assert.Null(request.ResolvedAt);
        Assert.Null(request.RejectionReason);
    }

    [Fact]
    public void RF_GP_04_Rechazar_con_motivo_guarda_motivo_recortado()
    {
        // Arrange
        var request = PermissionRequest.Create(RequesterId, "core.demo-action", Now);

        // Act
        request.Reject(AdministratorId, Now, "No corresponde");

        // Assert
        Assert.Equal(PermissionRequestStatus.Rejected, request.Status);
        Assert.Equal(AdministratorId, request.ApproverId);
        Assert.Equal(Now, request.ResolvedAt);
        Assert.Equal("No corresponde", request.RejectionReason);
    }

    [Fact]
    public void RF_GP_04_Rechazar_con_motivo_ausente_no_cambia_la_solicitud()
    {
        // Arrange
        var request = PermissionRequest.Create(RequesterId, "core.demo-action", Now);

        // Act
        var exception = Record.Exception(() => request.Reject(AdministratorId, Now, "   "));

        // Assert
        Assert.IsType<RejectionReasonRequiredException>(exception);
        Assert.Equal(PermissionRequestStatus.Pending, request.Status);
        Assert.Null(request.ApproverId);
        Assert.Null(request.ResolvedAt);
        Assert.Null(request.RejectionReason);
    }

    [Fact]
    public void RF_GP_06_Aplicar_una_rechazada_es_rechazado_y_conserva_Rejected()
    {
        // Arrange
        var request = PermissionRequest.Create(RequesterId, "core.demo-action", Now);
        request.Reject(AdministratorId, Now, "No corresponde");

        // Act
        var exception = Record.Exception(() => request.MarkApplied(Now));

        // Assert
        Assert.IsType<InvalidPermissionRequestTransitionException>(exception);
        Assert.Equal(PermissionRequestStatus.Rejected, request.Status);
        Assert.Equal(AdministratorId, request.ApproverId);
        Assert.Equal(Now, request.ResolvedAt);
        Assert.Equal("No corresponde", request.RejectionReason);
    }
}
