using SharedKernel.Exceptions;

namespace Wheelby.Domain.Reservations.Exceptions;

// 400: datos de la reserva inválidos al crearla.
public sealed class InvalidReservationDataException(string message)
    : BadRequestException(message);
