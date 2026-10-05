using SharedKernel.Exceptions;

namespace Wheelby.Domain.Reservations.Exceptions;

// 403: la transición existe pero este actor no puede ejecutarla o no cumple la condición.
public sealed class ReservationTransitionNotAllowedException(string message)
    : ForbiddenException(message);
