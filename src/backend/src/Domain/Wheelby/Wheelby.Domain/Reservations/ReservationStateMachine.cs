using Wheelby.Domain.Reservations.Exceptions;

namespace Wheelby.Domain.Reservations;

// RD-04: único lugar donde viven las transiciones permitidas.
// Agregar una transición es agregar una entrada a AllowedTransitions;
// Reservation.TransitionTo es el único código que cambia Status.
public static class ReservationStateMachine
{
    public sealed record TransitionDefinition(
        ReservationStatus From,
        ReservationStatus To,
        IReadOnlySet<ReservationActor> AllowedActors,
        string Condition,
        Func<Reservation, ReservationActor, DateTime, string?, bool>? Guard);

    // Las cinco transiciones permitidas.
    public static readonly IReadOnlyList<TransitionDefinition> AllowedTransitions =
    [
        new(
            ReservationStatus.Pending,
            ReservationStatus.Confirmed,
            new HashSet<ReservationActor> { ReservationActor.Host },
            "El anfitrión evaluó al inquilino.",
            static (r, _, utcNow, _) => utcNow <= r.StartDate),

        new(
            ReservationStatus.Pending,
            ReservationStatus.Cancelled,
            new HashSet<ReservationActor> { ReservationActor.Renter, ReservationActor.Host },
            "Motivo obligatorio si cancela el anfitrión; solo antes del inicio.",
            static (r, actor, utcNow, reason) =>
                utcNow < r.StartDate
                && (actor == ReservationActor.Renter || !string.IsNullOrWhiteSpace(reason))),

        new(
            ReservationStatus.Confirmed,
            ReservationStatus.InProgress,
            new HashSet<ReservationActor> { ReservationActor.Host },
            "El alquiler comienza; solo desde la fecha de inicio.",
            static (r, _, utcNow, _) => utcNow >= r.StartDate && utcNow <= r.EndDate),

        new(
            ReservationStatus.Confirmed,
            ReservationStatus.Cancelled,
            new HashSet<ReservationActor> { ReservationActor.Renter, ReservationActor.Host },
            "Motivo obligatorio si cancela el anfitrión; solo antes del inicio.",
            static (r, actor, utcNow, reason) =>
                utcNow < r.StartDate
                && (actor == ReservationActor.Renter || !string.IsNullOrWhiteSpace(reason))),

        new(
            ReservationStatus.InProgress,
            ReservationStatus.Completed,
            new HashSet<ReservationActor> { ReservationActor.Host },
            "El alquiler terminó.",
            static (r, _, utcNow, _) => utcNow >= r.EndDate),
    ];

    // RF-NEG-04: transición explícitamente prohibida con su motivo.
    // Cualquier otra combinación fuera de AllowedTransitions también se rechaza.
    public static readonly (ReservationStatus From, ReservationStatus To, string Reason) ExplicitlyForbidden =
        (ReservationStatus.InProgress, ReservationStatus.Cancelled,
            "Un alquiler en curso no se cancela; debe completarse.");

    // RF-NEG-05: estados terminales; ninguna transición parte de ellos.
    public static readonly IReadOnlySet<ReservationStatus> TerminalStates =
        new HashSet<ReservationStatus>
        {
            ReservationStatus.Completed,
            ReservationStatus.Cancelled
        };

    public static void Validate(
        Reservation reservation,
        ReservationStatus target,
        ReservationActor actor,
        DateTime utcNow,
        string? cancellationReason)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        var from = reservation.Status;

        if (TerminalStates.Contains(from))
            throw new InvalidReservationTransitionException(
                $"La reserva en estado {from} es terminal y no admite más transiciones.");

        if (from == ExplicitlyForbidden.From && target == ExplicitlyForbidden.To)
            throw new InvalidReservationTransitionException(
                $"Transición {from} → {target} prohibida: {ExplicitlyForbidden.Reason}");

        var definition = AllowedTransitions.FirstOrDefault(t => t.From == from && t.To == target);
        if (definition is null)
            throw new InvalidReservationTransitionException(
                $"Transición {from} → {target} no permitida.");

        if (!definition.AllowedActors.Contains(actor))
            throw new ReservationTransitionNotAllowedException(
                $"El actor {actor} no puede ejecutar la transición {from} → {target}.");

        if (definition.Guard is not null && !definition.Guard(reservation, actor, utcNow, cancellationReason))
            throw new ReservationTransitionNotAllowedException(
                $"No se cumple la condición de la transición {from} → {target}: {definition.Condition}");
    }
}
