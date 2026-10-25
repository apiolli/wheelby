using MediatR;
using Permissions.Application.Features.CreatePermissionRequest;
using Permissions.Contracts;
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
}
