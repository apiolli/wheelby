namespace Notifications.Contracts;

// Tipos de notificación (campo Type). Son estables: el cliente decide con ellos.
public static class NotificationTypes
{
    // Gestión de permisos (Core)
    public const string PermissionRequestCreated = "permission.request-created";
    public const string PermissionRequestApproved = "permission.request-approved";
    public const string PermissionRequestRejected = "permission.request-rejected";

    // Reservas (Wheelby)
    public const string ReservationRequested = "reservation.requested";
    public const string ReservationConfirmed = "reservation.confirmed";
    public const string ReservationRejected = "reservation.rejected";
    public const string ReservationCancelled = "reservation.cancelled";
    public const string ReservationExpired = "reservation.expired";
    public const string ReservationStarted = "reservation.started";
    public const string ReservationCompleted = "reservation.completed";
}
