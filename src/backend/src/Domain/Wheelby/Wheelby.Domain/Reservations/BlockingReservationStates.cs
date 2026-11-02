namespace Wheelby.Domain.Reservations;

// Estados de la reserva que bloquean el calendario (solapan fechas).
// Declarados una sola vez y reutilizados por repositorio y consultas.
public static class BlockingReservationStates
{
    public static readonly IReadOnlySet<ReservationStatus> States =
        new HashSet<ReservationStatus> { ReservationStatus.Confirmed, ReservationStatus.InProgress };
}
