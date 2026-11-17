using SharedKernel.Domain;
using Wheelby.Domain.Reservations.Events;
using Wheelby.Domain.Reservations.Exceptions;

namespace Wheelby.Domain.Reservations;

// Entidad central del módulo Wheelby. Su atributo Status nace en Pending
// y solo cambia a través de TransitionTo, que consulta a ReservationStateMachine.
public sealed class Reservation : AggregateRoot<Guid>
{
    public const int CancellationReasonMaxLength = 500;

    public Guid VehicleId { get; private set; }
    public Guid RenterId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public ReservationStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Precio congelado al crear la reserva: si el anfitrión cambia el precio
    // del vehículo después, el total de esta reserva no cambia.
    public decimal PricePerDayAtBooking { get; private set; }
    public int Days { get; private set; }
    public decimal TotalAmount { get; private set; }

    // Solo las filas agregadas en esta operación; el historial completo se lee
    // con IReservationReadStore. EF inserta las nuevas al guardar.
    private readonly List<ReservationStatusChange> _history = [];
    public IReadOnlyCollection<ReservationStatusChange> History => _history.AsReadOnly();

    private Reservation() : base(Guid.Empty) { }

    private Reservation(
        Guid id,
        Guid vehicleId,
        Guid renterId,
        DateTime startDate,
        DateTime endDate,
        decimal pricePerDay,
        DateTime utcNow)
        : base(id)
    {
        VehicleId = vehicleId;
        RenterId = renterId;
        StartDate = startDate;
        EndDate = endDate;
        Status = ReservationStatus.Pending;
        CreatedAt = utcNow;
        PricePerDayAtBooking = pricePerDay;
        Days = CountDays(startDate, endDate);
        TotalAmount = decimal.Round(pricePerDay * Days, 2, MidpointRounding.AwayFromZero);

        Record(null, ReservationStatus.Pending, ReservationActor.Renter, renterId, null, utcNow);
    }

    // Días cobrados: cada fracción de 24 h cuenta como un día completo.
    public static int CountDays(DateTime startDate, DateTime endDate)
        => Math.Max(1, (int)Math.Ceiling((endDate - startDate).TotalDays));

    public static Reservation Create(
        Guid vehicleId,
        Guid renterId,
        DateTime startDate,
        DateTime endDate,
        decimal pricePerDay,
        DateTime utcNow)
    {
        if (vehicleId == Guid.Empty)
            throw new InvalidReservationDataException("El vehículo es obligatorio.");

        if (renterId == Guid.Empty)
            throw new InvalidReservationDataException("El inquilino es obligatorio.");

        if (startDate.Kind != DateTimeKind.Utc || endDate.Kind != DateTimeKind.Utc)
            throw new InvalidReservationDataException("Las fechas deben estar en UTC.");

        if (startDate >= endDate)
            throw new InvalidReservationDataException("La fecha de inicio debe ser anterior a la de fin.");

        if (startDate <= utcNow)
            throw new InvalidReservationDataException("La fecha de inicio debe ser futura.");

        if (pricePerDay <= 0m)
            throw new InvalidReservationDataException("El precio por día debe ser mayor que cero.");

        return new Reservation(Guid.NewGuid(), vehicleId, renterId, startDate, endDate, pricePerDay, utcNow);
    }

    public void TransitionTo(
        ReservationStatus target,
        ReservationActor actor,
        DateTime utcNow,
        string? cancellationReason = null,
        Guid? actorId = null)
    {
        // Si la máquina rechaza, el estado no cambia.
        ReservationStateMachine.Validate(this, target, actor, utcNow, cancellationReason);

        var trimmed = cancellationReason?.Trim();
        var reason = string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;

        if (target == ReservationStatus.Cancelled)
            CancellationReason = reason;

        var from = Status;
        Status = target;
        Record(from, target, actor, actorId, reason, utcNow);
    }

    private void Record(
        ReservationStatus? from,
        ReservationStatus to,
        ReservationActor actor,
        Guid? actorId,
        string? reason,
        DateTime at)
    {
        _history.Add(new ReservationStatusChange(Id, from, to, actor, actorId, reason, at));
        AddDomainEvent(new ReservationStatusChanged(Id, from, to, actor, actorId, reason, at));
    }
}
