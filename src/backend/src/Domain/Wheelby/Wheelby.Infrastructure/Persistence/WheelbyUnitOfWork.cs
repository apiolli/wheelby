using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;

namespace Wheelby.Infrastructure.Persistence;

internal sealed class WheelbyUnitOfWork(WheelbyDbContext db) : IWheelbyUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("El registro fue modificado por otra operación. Vuelve a intentarlo.");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException("Ya existe un registro con esos datos.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
