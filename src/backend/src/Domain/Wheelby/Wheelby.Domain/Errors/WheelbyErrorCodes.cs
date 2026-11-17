namespace Wheelby.Domain.Errors;

// Códigos estables de error del módulo Wheelby. Viajan en problem+json como
// extensions.code; el frontend decide con ellos, no con el texto del mensaje.
// Un código publicado no se renombra: se agrega uno nuevo.
public static class WheelbyErrorCodes
{
    // Vehículos
    public const string VehicleNotFound = "vehicle.not-found";
    public const string VehicleInvalidData = "vehicle.invalid-data";
    public const string VehiclePlateTaken = "vehicle.plate-taken";
    public const string VehicleOverlap = "vehicle.overlap";
    public const string VehiclePublishNotAllowed = "vehicle.publish-not-allowed";

    // Reservas
    public const string ReservationNotFound = "reservation.not-found";
    public const string ReservationInvalidData = "reservation.invalid-data";
    public const string ReservationOwnVehicle = "reservation.own-vehicle";
    public const string ReservationNotParticipant = "reservation.not-participant";
    public const string ReservationNotHost = "reservation.not-host";
    public const string ReservationInvalidTransition = "reservation.invalid-transition";
    public const string ReservationTransitionNotAllowed = "reservation.transition-not-allowed";
}
