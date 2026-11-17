using SharedKernel.Domain;
using SharedKernel.Exceptions;
using Wheelby.Domain.Errors;
using Wheelby.Domain.Vehicles.Exceptions;

namespace Wheelby.Domain.Vehicles;

// Vehículo publicado por un miembro. Raíz de agregado; constructor privado para EF.
public sealed class Vehicle : AggregateRoot<Guid>
{
    public const int BrandMaxLength = 60;
    public const int ModelMaxLength = 60;
    public const int PlateMinLength = 3;
    public const int PlateMaxLength = 10;
    public const int CityMaxLength = 80;
    public const int DescriptionMaxLength = 1000;
    public const decimal PricePerDayMin = 0m;
    public const decimal PricePerDayMax = 1_000_000m;
    public const int YearMin = 1980;

    public Guid OwnerId { get; private set; }
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string Plate { get; private set; } = string.Empty;
    public decimal PricePerDay { get; private set; }
    public VehicleType Type { get; private set; }
    public string? City { get; private set; }
    public string? Description { get; private set; }

    // Un vehículo inactivo no aparece en la búsqueda ni acepta reservas nuevas.
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Vehicle() : base(Guid.Empty) { }

    private Vehicle(Guid id, Guid ownerId, DateTime utcNow) : base(id)
    {
        OwnerId = ownerId;
        CreatedAt = utcNow;
        IsActive = true;
    }

    public static Vehicle Create(
        Guid ownerId,
        string brand,
        string model,
        int year,
        string plate,
        decimal pricePerDay,
        DateTime utcNow,
        VehicleType type = VehicleType.Car,
        string? city = null,
        string? description = null)
    {
        if (ownerId == Guid.Empty)
            throw new InvalidVehicleDataException("El propietario es obligatorio.");

        if (utcNow.Kind != DateTimeKind.Utc)
            throw new InvalidVehicleDataException("La fecha debe estar en UTC.");

        var vehicle = new Vehicle(Guid.NewGuid(), ownerId, utcNow);
        vehicle.Apply(brand, model, year, plate, pricePerDay, type, city, description, utcNow);
        return vehicle;
    }

    // Edición del dueño. La placa nueva debe ser única (lo comprueba el caso de uso).
    // El precio nuevo no afecta reservas ya hechas: su total está congelado.
    public void Update(
        string brand,
        string model,
        int year,
        string plate,
        decimal pricePerDay,
        VehicleType type,
        string? city,
        string? description,
        DateTime utcNow)
    {
        Apply(brand, model, year, plate, pricePerDay, type, city, description, utcNow);
        UpdatedAt = utcNow;
    }

    // No se desactiva con reservas Confirmed o InProgress: el anfitrión ya se comprometió.
    public void Deactivate(bool hasBlockingReservations, DateTime utcNow)
    {
        if (hasBlockingReservations)
            throw new ConflictException(
                "No puedes desactivar un vehículo con reservas confirmadas o en curso.",
                WheelbyErrorCodes.VehicleHasActiveReservations);

        IsActive = false;
        UpdatedAt = utcNow;
    }

    public void Activate(DateTime utcNow)
    {
        IsActive = true;
        UpdatedAt = utcNow;
    }

    public static string NormalizePlate(string plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return string.Empty;

        var normalized = plate.Trim().ToUpperInvariant();

        foreach (var c in normalized)
        {
            if (!char.IsLetterOrDigit(c) && c != '-')
                throw new InvalidVehicleDataException("La placa solo puede contener letras, dígitos y guiones.");
        }

        return normalized;
    }

    private void Apply(
        string brand,
        string model,
        int year,
        string plate,
        decimal pricePerDay,
        VehicleType type,
        string? city,
        string? description,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(brand))
            throw new InvalidVehicleDataException("La marca es obligatoria.");

        if (brand.Trim().Length > BrandMaxLength)
            throw new InvalidVehicleDataException($"La marca no puede exceder los {BrandMaxLength} caracteres.");

        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidVehicleDataException("El modelo es obligatorio.");

        if (model.Trim().Length > ModelMaxLength)
            throw new InvalidVehicleDataException($"El modelo no puede exceder los {ModelMaxLength} caracteres.");

        if (year < YearMin || year > utcNow.Year + 1)
            throw new InvalidVehicleDataException($"El año debe estar entre {YearMin} y {utcNow.Year + 1}.");

        var normalizedPlate = NormalizePlate(plate);
        if (normalizedPlate.Length < PlateMinLength || normalizedPlate.Length > PlateMaxLength)
            throw new InvalidVehicleDataException($"La placa debe tener entre {PlateMinLength} y {PlateMaxLength} caracteres.");

        if (pricePerDay <= PricePerDayMin)
            throw new InvalidVehicleDataException("El precio por día debe ser mayor que cero.");

        if (pricePerDay > PricePerDayMax)
            throw new InvalidVehicleDataException($"El precio por día no puede exceder los {PricePerDayMax}.");

        if (!Enum.IsDefined(type))
            throw new InvalidVehicleDataException("El tipo de vehículo no es válido.");

        var normalizedCity = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        if (normalizedCity?.Length > CityMaxLength)
            throw new InvalidVehicleDataException($"La ciudad no puede exceder los {CityMaxLength} caracteres.");

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedDescription?.Length > DescriptionMaxLength)
            throw new InvalidVehicleDataException($"La descripción no puede exceder los {DescriptionMaxLength} caracteres.");

        Brand = brand.Trim();
        Model = model.Trim();
        Year = year;
        Plate = normalizedPlate;
        PricePerDay = pricePerDay;
        Type = type;
        City = normalizedCity;
        Description = normalizedDescription;
    }
}
