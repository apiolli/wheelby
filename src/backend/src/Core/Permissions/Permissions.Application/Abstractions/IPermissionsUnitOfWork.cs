namespace Permissions.Application.Abstractions;

// Unidad de trabajo del módulo: una sola transacción por operación.
public interface IPermissionsUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
