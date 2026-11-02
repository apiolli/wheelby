using AccessControl.Contracts;
using MediatR;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Features.RejectReservation;

internal sealed class RejectReservationCommandHandler(
    IVehicleRepository vehicles,
    IReservationRepository reservations,
    IWheelbyUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<RejectReservationCommand, Unit>
{
    public async Task<Unit> Handle(
        RejectReservationCommand command,
        CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("La reserva no existe.");

        var vehicle = await vehicles.GetByIdAsync(reservation.VehicleId, cancellationToken)
            ?? throw new NotFoundException("El vehículo de la reserva no existe.");

        // Solo el dueño del vehículo (actor Host) puede rechazar.
        if (vehicle.OwnerId != currentUser.Id)
            throw new ForbiddenException("Solo el dueño del vehículo puede rechazar la reserva.");

        reservation.TransitionTo(ReservationStatus.Cancelled, ReservationActor.Host, clock.UtcNow, command.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
