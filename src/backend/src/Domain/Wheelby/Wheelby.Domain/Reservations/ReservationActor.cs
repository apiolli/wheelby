namespace Wheelby.Domain.Reservations;

// Tipo de actor de negocio que ejecuta cada transición.
// El anfitrión es un perfil de negocio de Wheelby, no un rol del Core:
// la máquina verifica el tipo de actor; que el usuario concreto sea el
// dueño del vehículo o el inquilino de la reserva será trabajo de los casos de uso.
public enum ReservationActor
{
    Renter = 0,
    Host = 1
}
