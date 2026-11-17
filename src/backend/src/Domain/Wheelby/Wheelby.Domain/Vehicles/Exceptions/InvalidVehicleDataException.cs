using SharedKernel.Exceptions;
using Wheelby.Domain.Errors;

namespace Wheelby.Domain.Vehicles.Exceptions;

// Excepción de dominio (400) para datos inválidos de un vehículo.
public sealed class InvalidVehicleDataException : BadRequestException
{
    public InvalidVehicleDataException(string message) : base(message, WheelbyErrorCodes.VehicleInvalidData) { }
}
