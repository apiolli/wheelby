using AccessControl.Contracts;
using MediatR;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Features.CompleteRental;

internal sealed class CompleteRentalCommandHandler(
    IVehicleRepository vehicles,
    IReservationRepository reservations,
    IWheelbyUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<CompleteRentalCommand, Unit>
{
    public async Task<Unit> Handle(
        CompleteRentalCommand command,
        CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("La reserva no existe.");

        var vehicle = await vehicles.GetByIdAsync(reservation.VehicleId, cancellationToken)
            ?? throw new NotFoundException("El vehículo de la reserva no existe.");

        // Solo el dueño del vehículo (actor Host) puede completar el alquiler.
        if (vehicle.OwnerId != currentUser.Id)
            throw new ForbiddenException("Solo el dueño del vehículo puede completar el alquiler.");

        reservation.TransitionTo(ReservationStatus.Completed, ReservationActor.Host, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
