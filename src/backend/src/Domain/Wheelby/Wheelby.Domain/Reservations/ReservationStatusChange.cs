using SharedKernel.Domain;

namespace Wheelby.Domain.Reservations;

// Una fila del historial de estados de la reserva, para la línea de tiempo del
// detalle. Es un dato del negocio: no sustituye la auditoría del Core.
// From es null en la creación (→ Pending). ActorId es null cuando actúa el sistema.
public sealed class ReservationStatusChange : Entity<Guid>
{
    public Guid ReservationId { get; private set; }
    public ReservationStatus? From { get; private set; }
    public ReservationStatus To { get; private set; }
    public ReservationActor Actor { get; private set; }
    public Guid? ActorId { get; private set; }
    public string? Reason { get; private set; }
    public DateTime At { get; private set; }

    private ReservationStatusChange() { }

    internal ReservationStatusChange(
        Guid reservationId,
        ReservationStatus? from,
        ReservationStatus to,
        ReservationActor actor,
        Guid? actorId,
        string? reason,
        DateTime at)
        : base(Guid.NewGuid())
    {
        ReservationId = reservationId;
        From = from;
        To = to;
        Actor = actor;
        ActorId = actorId;
        Reason = reason;
        At = at;
    }
}
