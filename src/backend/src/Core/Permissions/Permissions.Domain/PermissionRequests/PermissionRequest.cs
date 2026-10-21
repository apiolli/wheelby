using Permissions.Domain.PermissionRequests.Exceptions;
using SharedKernel.Domain;

namespace Permissions.Domain.PermissionRequests;

// Entidad central del módulo Permissions. Su atributo Status nace en Pending
// y solo cambia a través de TransitionTo, que consulta a PermissionRequestStateMachine.
public sealed class PermissionRequest : AggregateRoot<Guid>
{
    public Guid RequesterId { get; private set; }
    public string PermissionCode { get; private set; } = default!;
    public PermissionRequestStatus Status { get; private set; }
    public Guid? ApproverId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    private PermissionRequest() : base(Guid.Empty) { }

    private PermissionRequest(Guid id, Guid requesterId, string permissionCode, DateTime utcNow)
        : base(id)
    {
        RequesterId = requesterId;
        PermissionCode = permissionCode;
        Status = PermissionRequestStatus.Pending;
        CreatedAt = utcNow;
    }

    public static PermissionRequest Create(Guid requesterId, string permissionCode, DateTime utcNow)
    {
        if (requesterId == Guid.Empty)
            throw new InvalidPermissionRequestDataException("El solicitante es obligatorio.");

        if (string.IsNullOrWhiteSpace(permissionCode))
            throw new InvalidPermissionRequestDataException("El código de permiso es obligatorio.");

        if (!PermissionCodeRules.IsValid(permissionCode))
            throw new InvalidPermissionRequestDataException("El código de permiso no tiene un formato válido.");

        if (utcNow.Kind != DateTimeKind.Utc)
            throw new InvalidPermissionRequestDataException("La fecha debe estar en UTC.");

        return new PermissionRequest(Guid.NewGuid(), requesterId, permissionCode, utcNow);
    }
}
