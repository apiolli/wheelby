using Permissions.Domain.PermissionGrants.Exceptions;
using Permissions.Domain.PermissionRequests;
using SharedKernel.Domain;

namespace Permissions.Domain.PermissionGrants;

// Concesión aplicada: el usuario ya tiene el permiso.
public sealed class PermissionGrant : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string PermissionCode { get; private set; } = default!;
    public DateTime GrantedAt { get; private set; }
    public Guid RequestId { get; private set; }

    private PermissionGrant() : base(Guid.Empty) { }

    private PermissionGrant(Guid id, Guid userId, string permissionCode, Guid requestId, DateTime utcNow)
        : base(id)
    {
        UserId = userId;
        PermissionCode = permissionCode;
        RequestId = requestId;
        GrantedAt = utcNow;
    }

    public static PermissionGrant Create(
        Guid userId,
        string permissionCode,
        Guid requestId,
        DateTime utcNow)
    {
        if (userId == Guid.Empty)
            throw new InvalidPermissionGrantDataException("El usuario es obligatorio.");

        if (string.IsNullOrWhiteSpace(permissionCode))
            throw new InvalidPermissionGrantDataException("El código de permiso es obligatorio.");

        if (!PermissionCodeRules.IsValid(permissionCode))
            throw new InvalidPermissionGrantDataException("El código de permiso no tiene un formato válido.");

        if (requestId == Guid.Empty)
            throw new InvalidPermissionGrantDataException("La solicitud es obligatoria.");

        if (utcNow.Kind != DateTimeKind.Utc)
            throw new InvalidPermissionGrantDataException("La fecha debe estar en UTC.");

        return new PermissionGrant(Guid.NewGuid(), userId, permissionCode, requestId, utcNow);
    }
}
