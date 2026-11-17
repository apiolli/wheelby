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
    public const string VehicleNotOwner = "vehicle.not-owner";
    public const string VehicleInactive = "vehicle.inactive";
    public const string VehicleHasActiveReservations = "vehicle.has-active-reservations";
    public const string VehiclePhotoInvalid = "vehicle.photo-invalid";
    public const string VehiclePhotoLimit = "vehicle.photo-limit";
    public const string VehiclePhotoNotFound = "vehicle.photo-not-found";

    // Documentos (de Documents, usados por Wheelby)
    public const string DocumentNotFound = "document.not-found";
    public const string DocumentNotOwner = "document.not-owner";

    // Reservas
    public const string ReservationNotFound = "reservation.not-found";
    public const string ReservationInvalidData = "reservation.invalid-data";
    public const string ReservationOwnVehicle = "reservation.own-vehicle";
    public const string ReservationNotParticipant = "reservation.not-participant";
    public const string ReservationNotHost = "reservation.not-host";
    public const string ReservationInvalidTransition = "reservation.invalid-transition";
    public const string ReservationTransitionNotAllowed = "reservation.transition-not-allowed";
    public const string ReservationDocumentNotAllowed = "reservation.document-not-allowed";
    public const string ReservationDocumentDuplicate = "reservation.document-duplicate";
    public const string ReservationDocumentLimit = "reservation.document-limit";

    // Reseñas
    public const string ReviewNotAllowed = "review.not-allowed";
    public const string ReviewInvalidData = "review.invalid-data";
    public const string ReviewDuplicate = "review.duplicate";
}
