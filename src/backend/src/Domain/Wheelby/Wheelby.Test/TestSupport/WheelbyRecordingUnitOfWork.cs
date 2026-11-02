using Wheelby.Application.Abstractions;

namespace Wheelby.Test.TestSupport;

// Unidad de trabajo en memoria que cuenta cuántas veces se guarda.
public sealed class WheelbyRecordingUnitOfWork : IWheelbyUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
