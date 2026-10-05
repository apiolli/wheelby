using SharedKernel.Exceptions;

namespace Wheelby.Domain.Reservations.Exceptions;

// 409: el par (origen, destino) no existe en la tabla o parte de un estado terminal.
public sealed class InvalidReservationTransitionException(string message)
    : ConflictException(message);
