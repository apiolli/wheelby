using AccessControl.Contracts;
using MediatR;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Domain.Errors;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Features.ConfirmReservation;

internal sealed class ConfirmReservationCommandHandler(
    IVehicleRepository vehicles,
    IReservationRepository reservations,
    IWheelbyUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<ConfirmReservationCommand, Unit>
{
    public async Task<Unit> Handle(
        ConfirmReservationCommand command,
        CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("La reserva no existe.", WheelbyErrorCodes.ReservationNotFound);

        var vehicle = await vehicles.GetByIdAsync(reservation.VehicleId, cancellationToken)
            ?? throw new NotFoundException("El vehículo de la reserva no existe.", WheelbyErrorCodes.VehicleNotFound);

        // Solo el dueño del vehículo (actor Host) puede confirmar.
        if (vehicle.OwnerId != currentUser.Id)
            throw new ForbiddenException("Solo el dueño del vehículo puede confirmar la reserva.", WheelbyErrorCodes.ReservationNotHost);

        if (!vehicle.IsActive)
            throw new ConflictException(
                "El vehículo está desactivado; actívalo o rechaza la solicitud.", WheelbyErrorCodes.VehicleInactive);

        // Nueva comprobación de solape bloqueante, excluyendo la propia reserva.
        var hasOverlap = await reservations.HasBlockingOverlapAsync(
            reservation.VehicleId,
            reservation.StartDate,
            reservation.EndDate,
            excludeReservationId: reservation.Id,
            cancellationToken: cancellationToken);

        if (hasOverlap)
            throw new ConflictException("El vehículo ya tiene una reserva bloqueante que solapa esas fechas.", WheelbyErrorCodes.VehicleOverlap);

        reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, clock.UtcNow, actorId: currentUser.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
