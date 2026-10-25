using MediatR;
using Permissions.Application.Features.CreatePermissionRequest;
using Permissions.Contracts;
using Permissions.Domain.PermissionGrants;
using Permissions.Domain.PermissionRequests;
using Permissions.Test.Support;
using SharedKernel.Exceptions;

namespace Permissions.Test.Application;

public sealed class CreatePermissionRequestHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid SolicitanteId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task RF_GP_01_Crear_guarda_una_vez_y_devuelve_el_id()
    {
        // Arrange
        var requests = new FakePermissionRequestRepository();
        var grants = new FakePermissionGrantRepository();
        var catalog = new FixedPermissionCatalog(PermissionCodes.DemoAction);
        var unitOfWork = new RecordingUnitOfWork();
        var currentUser = new FixedCurrentUser(SolicitanteId);
        var handler = new CreatePermissionRequestCommandHandler(
            requests, grants, catalog, unitOfWork, currentUser, new FixedClock(Ahora));

        // Act
        var id = await handler.Handle(
            new CreatePermissionRequestCommand(PermissionCodes.DemoAction), default);

        // Assert
        var request = Assert.Single(requests.Requests);
        Assert.Equal(id, request.Id);
        Assert.Equal(PermissionRequestStatus.Pending, request.Status);
        Assert.Equal(SolicitanteId, request.RequesterId);
        Assert.Equal(PermissionCodes.DemoAction, request.PermissionCode);
        Assert.Equal(Ahora, request.CreatedAt);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_GP_01_Crear_con_codigo_inexistente_es_rechazado()
    {
        // Arrange
        var requests = new FakePermissionRequestRepository();
        var grants = new FakePermissionGrantRepository();
        var catalog = new FixedPermissionCatalog(PermissionCodes.DemoAction);
        var unitOfWork = new RecordingUnitOfWork();
        var currentUser = new FixedCurrentUser(SolicitanteId);
        var handler = new CreatePermissionRequestCommandHandler(
            requests, grants, catalog, unitOfWork, currentUser, new FixedClock(Ahora));

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new CreatePermissionRequestCommand("core.otro.accion"), default));

        // Assert
        Assert.IsType<BadRequestException>(exception);
        Assert.Empty(requests.Requests);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_GP_01_Crear_con_solicitud_pendiente_duplicada_es_rechazado()
    {
        // Arrange
        var pendiente = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        var requests = new FakePermissionRequestRepository(pendiente);
        var grants = new FakePermissionGrantRepository();
        var catalog = new FixedPermissionCatalog(PermissionCodes.DemoAction);
        var unitOfWork = new RecordingUnitOfWork();
        var currentUser = new FixedCurrentUser(SolicitanteId);
        var handler = new CreatePermissionRequestCommandHandler(
            requests, grants, catalog, unitOfWork, currentUser, new FixedClock(Ahora));

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(
                new CreatePermissionRequestCommand(PermissionCodes.DemoAction), default));

        // Assert
        Assert.IsType<ConflictException>(exception);
        Assert.Single(requests.Requests);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_GP_01_Crear_con_concesion_existente_es_rechazado()
    {
        // Arrange
        var solicitud = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        var concesion = PermissionGrant.Create(
            SolicitanteId, PermissionCodes.DemoAction, solicitud.Id, Ahora);
        var requests = new FakePermissionRequestRepository(solicitud);
        var grants = new FakePermissionGrantRepository(concesion);
        var catalog = new FixedPermissionCatalog(PermissionCodes.DemoAction);
        var unitOfWork = new RecordingUnitOfWork();
        var currentUser = new FixedCurrentUser(SolicitanteId);
        var handler = new CreatePermissionRequestCommandHandler(
            requests, grants, catalog, unitOfWork, currentUser, new FixedClock(Ahora));

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(
                new CreatePermissionRequestCommand(PermissionCodes.DemoAction), default));

        // Assert
        Assert.IsType<ConflictException>(exception);
        Assert.Single(requests.Requests);
        Assert.Equal([concesion], grants.Grants);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_GP_01_Crear_tras_una_rechazada_es_permitido()
    {
        // Arrange
        var rechazada = PermissionRequest.Create(SolicitanteId, PermissionCodes.DemoAction, Ahora);
        rechazada.Reject(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Ahora.AddMinutes(5),
            "No corresponde");
        var requests = new FakePermissionRequestRepository(rechazada);
        var grants = new FakePermissionGrantRepository();
        var catalog = new FixedPermissionCatalog(PermissionCodes.DemoAction);
        var unitOfWork = new RecordingUnitOfWork();
        var currentUser = new FixedCurrentUser(SolicitanteId);
        var handler = new CreatePermissionRequestCommandHandler(
            requests, grants, catalog, unitOfWork, currentUser, new FixedClock(Ahora));

        // Act
        var id = await handler.Handle(
            new CreatePermissionRequestCommand(PermissionCodes.DemoAction), default);

        // Assert
        Assert.Equal(2, requests.Requests.Count);
        Assert.NotEqual(rechazada.Id, id);
        Assert.Equal(PermissionRequestStatus.Pending, requests.Requests.Last().Status);
        Assert.Equal(Ahora, requests.Requests.Last().CreatedAt);
        Assert.Equal(1, unitOfWork.SaveCount);
    }
}
