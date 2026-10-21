namespace Permissions.Domain.PermissionRequests;

// RF-GP-01: único lugar donde se declaran los estados de la solicitud de permiso.
public enum PermissionRequestStatus
{
    Pending = 0,
    Approved = 1,
    Applied = 2,
    Rejected = 3
}
