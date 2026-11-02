using SharedKernel.Domain;
using Wheelby.Domain.Vehicles.Exceptions;

namespace Wheelby.Domain.Vehicles;

// Vehículo publicado por un miembro. Raíz de agregado; constructor privado para EF.
public sealed class Vehicle : AggregateRoot<Guid>
{
    public const int BrandMaxLength = 60;
    public const int ModelMaxLength = 60;
    public const int PlateMinLength = 3;
    public const int PlateMaxLength = 10;
    public const decimal PricePerDayMin = 0m;
    public const decimal PricePerDayMax = 1_000_000m;
    public const int YearMin = 1980;

    public Guid OwnerId { get; private set; }
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string Plate { get; private set; } = string.Empty;
    public decimal PricePerDay { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Vehicle() : base(Guid.Empty) { }

    private Vehicle(
        Guid id,
        Guid ownerId,
        string brand,
        string model,
        int year,
        string plate,
        decimal pricePerDay,
        DateTime utcNow)
        : base(id)
    {
        OwnerId = ownerId;
        Brand = brand;
        Model = model;
        Year = year;
        Plate = plate;
        PricePerDay = pricePerDay;
        CreatedAt = utcNow;
    }

    public static Vehicle Create(
        Guid ownerId,
        string brand,
        string model,
        int year,
        string plate,
        decimal pricePerDay,
        DateTime utcNow)
    {
        if (ownerId == Guid.Empty)
            throw new InvalidVehicleDataException("El propietario es obligatorio.");

        if (string.IsNullOrWhiteSpace(brand))
            throw new InvalidVehicleDataException("La marca es obligatoria.");

        if (brand.Length > BrandMaxLength)
            throw new InvalidVehicleDataException($"La marca no puede exceder los {BrandMaxLength} caracteres.");

        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidVehicleDataException("El modelo es obligatorio.");

        if (model.Length > ModelMaxLength)
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

        if (utcNow.Kind != DateTimeKind.Utc)
            throw new InvalidVehicleDataException("La fecha debe estar en UTC.");

        return new Vehicle(
            Guid.NewGuid(),
            ownerId,
            brand.Trim(),
            model.Trim(),
            year,
            normalizedPlate,
            pricePerDay,
            utcNow);
    }

    private static string NormalizePlate(string plate)
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
}
