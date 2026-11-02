using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Abstractions;

// Puerto de escritura del agregado Vehicle.
public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> PlateExistsAsync(string plate, CancellationToken cancellationToken = default);
    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
}
