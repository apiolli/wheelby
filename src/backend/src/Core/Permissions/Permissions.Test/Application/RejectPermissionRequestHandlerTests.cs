using AccessControl.Contracts;
using MediatR;
using Permissions.Application.Features.RejectPermissionRequest;
using Permissions.Contracts;
using Permissions.Domain.PermissionRequests;
using Permissions.Domain.PermissionRequests.Exceptions;
using Permissions.Test.Support;

namespace Permissions.Test.Application;

public sealed class RejectPermissionRequestHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Despues = Ahora.AddMinutes(5);
    private static readonly DateTime MasTarde = Ahora.AddMinutes(10);
    private static readonly Guid SolicitanteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdministradorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static RejectPermissionRequestCommandHandler ConDobles(
        FakePermissionRequestRepository requests,
        RecordingUnitOfWork unitOfWork,
        string rol)
        => new(requests, unitOfWork, new FixedCurrentUser(AdministradorId, rol), new FixedClock(Despues));

    [Fact]
    public async Task RF_GP_04_Rechazar_guarda_un_motivo_recortado()
    {
        // Arrange
        var solicitud = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        var requests = new FakePermissionRequestRepository(solicitud);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = ConDobles(requests, unitOfWork, AccessControlRoles.Administrator);

        // Act
        await handler.Handle(
            new RejectPermissionRequestCommand(solicitud.Id, "  No corresponde  "), default);

        // Assert
        Assert.Equal(PermissionRequestStatus.Rejected, solicitud.Status);
        Assert.Equal(AdministradorId, solicitud.ApproverId);
        Assert.Equal(Despues, solicitud.ResolvedAt);
        Assert.Equal("No corresponde", solicitud.RejectionReason);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RF_GP_04_Deja_la_solicitud_y_los_datos_sin_cambios_sin_motivo(string? motivo)
    {
        // Arrange
        var solicitud = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        var requests = new FakePermissionRequestRepository(solicitud);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = ConDobles(requests, unitOfWork, AccessControlRoles.Administrator);

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new RejectPermissionRequestCommand(solicitud.Id, motivo), default));

        // Assert
        Assert.IsType<RejectionReasonRequiredException>(exception);
        Assert.Equal(PermissionRequestStatus.Pending, solicitud.Status);
        Assert.Null(solicitud.ApproverId);
        Assert.Null(solicitud.ResolvedAt);
        Assert.Null(solicitud.RejectionReason);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_GP_07_Rechazar_la_propia_solicitud_es_rechazado()
    {
        // Arrange
        var solicitud = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        var requests = new FakePermissionRequestRepository(solicitud);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new RejectPermissionRequestCommandHandler(
            requests, unitOfWork, new FixedCurrentUser(SolicitanteId), new FixedClock(Despues));

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(
                new RejectPermissionRequestCommand(solicitud.Id, "Mi propio motivo"), default));

        // Assert
        Assert.IsType<CannotResolveOwnPermissionRequestException>(exception);
        Assert.Equal(PermissionRequestStatus.Pending, solicitud.Status);
        Assert.Null(solicitud.ApproverId);
        Assert.Null(solicitud.ResolvedAt);
        Assert.Null(solicitud.RejectionReason);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_GP_07_Rechazar_la_propia_resuelta_prioriza_403()
    {
        // Arrange
        var solicitud = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        solicitud.Approve(AdministradorId, Despues);
        solicitud.MarkApplied(Despues);
        var requests = new FakePermissionRequestRepository(solicitud);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new RejectPermissionRequestCommandHandler(
            requests, unitOfWork, new FixedCurrentUser(SolicitanteId), new FixedClock(MasTarde));

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(
                new RejectPermissionRequestCommand(solicitud.Id, "Mi propio motivo"), default));

        // Assert
        // Aun resuelta, la propia gana 403 antes de llegar al 409 de estado.
        Assert.IsType<CannotResolveOwnPermissionRequestException>(exception);
        Assert.Equal(PermissionRequestStatus.Applied, solicitud.Status);
        Assert.Equal(Despues, solicitud.ResolvedAt);
        Assert.Null(solicitud.RejectionReason);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_GP_06_Rechazar_una_aprobada_es_rechazado()
    {
        // Arrange
        var solicitud = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        solicitud.Approve(AdministradorId, Despues);
        var requests = new FakePermissionRequestRepository(solicitud);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = ConDobles(requests, unitOfWork, AccessControlRoles.Administrator);

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(
                new RejectPermissionRequestCommand(solicitud.Id, "No corresponde"), default));

        // Assert
        Assert.IsType<InvalidPermissionRequestTransitionException>(exception);
        Assert.Equal(PermissionRequestStatus.Approved, solicitud.Status);
        Assert.Equal(AdministradorId, solicitud.ApproverId);
        Assert.Equal(Despues, solicitud.ResolvedAt);
        Assert.Null(solicitud.RejectionReason);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_GP_06_Rechazar_una_aplicada_es_rechazado()
    {
        // Arrange
        var solicitud = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        solicitud.Approve(AdministradorId, Despues);
        solicitud.MarkApplied(Despues);
        var requests = new FakePermissionRequestRepository(solicitud);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = ConDobles(requests, unitOfWork, AccessControlRoles.Administrator);

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(
                new RejectPermissionRequestCommand(solicitud.Id, "No corresponde"), default));

        // Assert
        Assert.IsType<InvalidPermissionRequestTransitionException>(exception);
        Assert.Equal(PermissionRequestStatus.Applied, solicitud.Status);
        Assert.Equal(AdministradorId, solicitud.ApproverId);
        Assert.Equal(Despues, solicitud.ResolvedAt);
        Assert.Null(solicitud.RejectionReason);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_GP_06_Rechazar_una_rechazada_es_rechazado()
    {
        // Arrange
        var solicitud = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        solicitud.Reject(AdministradorId, Despues, "No corresponde");
        var requests = new FakePermissionRequestRepository(solicitud);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = ConDobles(requests, unitOfWork, AccessControlRoles.Administrator);

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(
                new RejectPermissionRequestCommand(solicitud.Id, "Otra razon"), default));

        // Assert
        Assert.IsType<InvalidPermissionRequestTransitionException>(exception);
        Assert.Equal(PermissionRequestStatus.Rejected, solicitud.Status);
        Assert.Equal("No corresponde", solicitud.RejectionReason);
        Assert.Equal(Despues, solicitud.ResolvedAt);
        Assert.Equal(0, unitOfWork.SaveCount);
    }
}
