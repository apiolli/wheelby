using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Domain;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Errors;

namespace Wheelby.Infrastructure.Persistence;

internal sealed class WheelbyUnitOfWork(WheelbyDbContext db, IPublisher publisher) : IWheelbyUnitOfWork
{
    // Nombre de la restricción EXCLUDE de la migración NoBlockingOverlap.
    public const string NoBlockingOverlapConstraint = "no_blocking_overlap";

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // Los eventos se toman antes de guardar y se publican después: un handler
        // solo ve cambios que ya están en la base.
        var aggregates = db.ChangeTracker.Entries<AggregateRoot<Guid>>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();
        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("El registro fue modificado por otra operación. Vuelve a intentarlo.");
        }
        catch (DbUpdateException ex) when (IsBlockingOverlap(ex))
        {
            // Dos confirmaciones simultáneas: la base rechaza la segunda.
            throw new ConflictException(
                "El vehículo ya tiene una reserva bloqueante que solapa esas fechas.", WheelbyErrorCodes.VehicleOverlap);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException("Ya existe un registro con esos datos.");
        }

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        foreach (var domainEvent in events)
            await publisher.Publish(domainEvent, cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static bool IsBlockingOverlap(DbUpdateException exception)
        => exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ExclusionViolation,
            ConstraintName: NoBlockingOverlapConstraint
        };
}
