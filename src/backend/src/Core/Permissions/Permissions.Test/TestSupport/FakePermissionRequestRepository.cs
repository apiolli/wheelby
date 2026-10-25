using Permissions.Application.Abstractions;
using Permissions.Domain.PermissionRequests;

namespace Permissions.Test.Support;

// Doble en memoria del repositorio de solicitudes: lista propia de cada prueba.
public sealed class FakePermissionRequestRepository(params PermissionRequest[] seed)
    : IPermissionRequestRepository
{
    public List<PermissionRequest> Requests { get; } = [.. seed];

    public Task<PermissionRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Requests.FirstOrDefault(r => r.Id == id));

    public Task<bool> ExistsPendingAsync(
        Guid requesterId, string permissionCode, CancellationToken cancellationToken)
        => Task.FromResult(Requests.Any(r =>
            r.RequesterId == requesterId
            && r.PermissionCode == permissionCode
            && r.Status == PermissionRequestStatus.Pending));

    public Task AddAsync(PermissionRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.CompletedTask;
    }
}
