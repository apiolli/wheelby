using Microsoft.EntityFrameworkCore;
using Npgsql;
using Permissions.Application.Abstractions;
using SharedKernel.Exceptions;

namespace Permissions.Infrastructure.Persistence;

internal sealed class PermissionsUnitOfWork(PermissionsDbContext db) : IPermissionsUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // La fila cambió entre la lectura y el guardado (xmin).
            throw new ConflictException(
                "La solicitud fue modificada por otra operación. Vuelve a intentarlo.");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Carrera entre dos solicitudes simultáneas o dos concesiones del mismo permiso.
            throw new ConflictException("Ya existe una solicitud o una concesión con esos datos.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
