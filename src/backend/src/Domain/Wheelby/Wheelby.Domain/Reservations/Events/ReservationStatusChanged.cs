using SharedKernel.Domain;

namespace Wheelby.Domain.Reservations.Events;

// Se publica después de guardar cada cambio de estado, incluida la creación
// (From = null). El motor de notificaciones lo convierte en aviso (RF-NEG-07, RF-NOT-03).
// IsRejection distingue el rechazo de una solicitud (/reject) de una cancelación del
// anfitrión (/cancel): las dos son Pending -> Cancelled con actor Host.
public sealed record ReservationStatusChanged(
    Guid ReservationId,
    ReservationStatus? From,
    ReservationStatus To,
    ReservationActor Actor,
    Guid? ActorId,
    string? Reason,
    DateTime At,
    bool IsRejection = false) : IDomainEvent;
