using MediatR;
using Permissions.Application.Features.ApprovePermissionRequest;
using Permissions.Contracts;
using Permissions.Domain.PermissionRequests;
using Permissions.Domain.PermissionRequests.Exceptions;
using Permissions.Test.Support;

namespace Permissions.Test.Application;

public sealed class ApprovePermissionRequestHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Despues = Ahora.AddMinutes(5);
    private static readonly DateTime MasTarde = Ahora.AddMinutes(10);
    private static readonly Guid SolicitanteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdministradorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task RF_GP_03_Aprobar_registra_el_aprobador_y_la_fecha_del_reloj()
    {
        // Arrange
        var solicitud = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        var handler = CambiaCon(
            new FakePermissionRequestRepository(solicitud),
            new FakePermissionGrantRepository(),
            new RecordingUnitOfWork(),
            new FixedCurrentUser(AdministradorId),
            new FixedClock(Despues));

        // Act
        await handler.Handle(new ApprovePermissionRequestCommand(solicitud.Id), default);

        // Assert
        Assert.Equal(AdministradorId, solicitud.ApproverId);
        Assert.NotEqual(SolicitanteId, solicitud.ApproverId);
        Assert.Equal(Despues, solicitud.ResolvedAt);
        Assert.NotEqual(Ahora, solicitud.ResolvedAt);
    }

    [Fact]
    public async Task RF_GP_05_Aprobar_deja_la_solicitud_en_Applied()
    {
        // Arrange
        var solicitud = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        var requests = new FakePermissionRequestRepository(solicitud);
        var grants = new FakePermissionGrantRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CambiaCon(
            requests, grants, unitOfWork,
            new FixedCurrentUser(AdministradorId), new FixedClock(Despues));

        // Act
        await handler.Handle(new ApprovePermissionRequestCommand(solicitud.Id), default);

        // Assert
        Assert.Equal(PermissionRequestStatus.Applied, solicitud.Status);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_GP_05_Aprobar_crea_la_concesion_del_solicitante()
    {
        // Arrange
        var solicitud = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        var requests = new FakePermissionRequestRepository(solicitud);
        var grants = new FakePermissionGrantRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CambiaCon(
            requests, grants, unitOfWork,
            new FixedCurrentUser(AdministradorId), new FixedClock(Despues));

        // Act
        await handler.Handle(new ApprovePermissionRequestCommand(solicitud.Id), default);

        // Assert
        var concesion = Assert.Single(grants.Grants);
        Assert.Equal(SolicitanteId, concesion.UserId);
        Assert.NotEqual(AdministradorId, concesion.UserId);
        Assert.Equal(PermissionCodes.DemoAction, concesion.PermissionCode);
        Assert.Equal(solicitud.Id, concesion.RequestId);
        Assert.Equal(Despues, concesion.GrantedAt);
    }

    [Fact]
    public async Task RF_GP_06_Aprobar_dos_veces_no_crea_segunda_concesion()
    {
        // Arrange
        var solicitud = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        var requests = new FakePermissionRequestRepository(solicitud);
        var grants = new FakePermissionGrantRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CambiaCon(
            requests, grants, unitOfWork,
            new FixedCurrentUser(AdministradorId), new FixedClock(Despues));

        // Act
        await handler.Handle(new ApprovePermissionRequestCommand(solicitud.Id), default);
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new ApprovePermissionRequestCommand(solicitud.Id), default));

        // Assert
        Assert.IsType<InvalidPermissionRequestTransitionException>(exception);
        Assert.Single(grants.Grants);
        Assert.Equal(PermissionRequestStatus.Applied, solicitud.Status);
        Assert.Equal(AdministradorId, solicitud.ApproverId);
        Assert.Equal(Despues, solicitud.ResolvedAt);
        Assert.Null(solicitud.RejectionReason);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_GP_06_Aprobar_una_solicitud_Applied_es_rechazado()
    {
        // Arrange
        var solicitud = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        solicitud.Approve(AdministradorId, Despues);
        solicitud.MarkApplied(Despues);
        var requests = new FakePermissionRequestRepository(solicitud);
        var grants = new FakePermissionGrantRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CambiaCon(
            requests, grants, unitOfWork,
            new FixedCurrentUser(AdministradorId), new FixedClock(MasTarde));

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new ApprovePermissionRequestCommand(solicitud.Id), default));

        // Assert
        Assert.IsType<InvalidPermissionRequestTransitionException>(exception);
        Assert.Empty(grants.Grants);
        Assert.Equal(PermissionRequestStatus.Applied, solicitud.Status);
        Assert.Equal(AdministradorId, solicitud.ApproverId);
        Assert.Equal(Despues, solicitud.ResolvedAt);
        Assert.Null(solicitud.RejectionReason);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private static ApprovePermissionRequestCommandHandler CambiaCon(
        FakePermissionRequestRepository requests,
        FakePermissionGrantRepository grants,
        RecordingUnitOfWork unitOfWork,
        FixedCurrentUser currentUser,
        FixedClock clock)
        => new(requests, grants, unitOfWork, currentUser, clock);
}
