using Wheelby.Application.Abstractions;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Test.TestSupport;

// Repositorio en memoria de vehículos para pruebas unitarias de casos de uso.
public sealed class InMemoryVehicleRepository : IVehicleRepository
{
    private readonly List<Vehicle> _vehicles = [];

    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_vehicles.FirstOrDefault(v => v.Id == id));

    public Task<bool> PlateExistsAsync(string plate, CancellationToken cancellationToken = default)
        => Task.FromResult(_vehicles.Any(v => string.Equals(v.Plate, plate, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        _vehicles.Add(vehicle);
        return Task.CompletedTask;
    }

    public IReadOnlyList<Vehicle> Vehicles => _vehicles.AsReadOnly();
}
