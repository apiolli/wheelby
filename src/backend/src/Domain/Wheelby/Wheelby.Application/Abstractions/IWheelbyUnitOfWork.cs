namespace Wheelby.Application.Abstractions;

// Unidad de trabajo del módulo: una sola transacción por operación.
public interface IWheelbyUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
