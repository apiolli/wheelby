namespace Wheelby.Domain.Reservations;

// RF-NEG-03: único lugar donde se declaran los estados de la Reserva (entre 3 y 5).
public enum ReservationStatus
{
    Pending = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}
