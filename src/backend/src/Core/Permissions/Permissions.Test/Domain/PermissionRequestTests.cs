using Permissions.Domain.PermissionRequests;

namespace Permissions.Test.Domain;

public sealed class PermissionRequestTests
{
    private static readonly DateTime Now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid RequesterId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdministratorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Crear_solicitud_nace_en_Pending()
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
}
