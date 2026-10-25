using Permissions.Application.Abstractions;

namespace Permissions.Test.Support;

// Unidad de trabajo que cuenta las llamadas a SaveChanges.
public sealed class RecordingUnitOfWork : IPermissionsUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
