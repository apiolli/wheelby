using SharedKernel.Domain;

namespace Wheelby.Domain.Reservations.Events;

// Se publica después de guardar cada cambio de estado, incluida la creación
// (From = null). Notificaciones lo convierte en correo (RF-NEG-07).
public sealed record ReservationStatusChanged(
    Guid ReservationId,
    ReservationStatus? From,
    ReservationStatus To,
    ReservationActor Actor,
    Guid? ActorId,
    string? Reason,
    DateTime At) : IDomainEvent;
