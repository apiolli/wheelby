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

    // Único punto que cambia Status (RD-04).
    public void TransitionTo(
        PermissionRequestStatus target,
        PermissionActor actor,
        Guid resolverId,
        DateTime utcNow,
        string? rejectionReason = null)
    {
        // Si la máquina rechaza, el estado no cambia.
        PermissionRequestStateMachine.Validate(this, target, actor, resolverId, utcNow, rejectionReason);

        if (target == PermissionRequestStatus.Approved)
        {
            ApproverId = resolverId;
            ResolvedAt = utcNow;
        }

        if (target == PermissionRequestStatus.Rejected)
        {
            ApproverId = resolverId;
            ResolvedAt = utcNow;
        }

        Status = target;
    }

    public void Approve(Guid approverId, DateTime utcNow)
        => TransitionTo(PermissionRequestStatus.Approved, PermissionActor.Administrator, approverId, utcNow);

    public void Reject(Guid approverId, DateTime utcNow, string? rejectionReason)
        => TransitionTo(PermissionRequestStatus.Rejected, PermissionActor.Administrator, approverId, utcNow, rejectionReason);

    public void MarkApplied(DateTime utcNow)
        => TransitionTo(PermissionRequestStatus.Applied, PermissionActor.System, Guid.Empty, utcNow);
}
