using Documents.Application.Abstractions;

namespace Documents.Test.TestSupport;

// Cuenta los guardados y puede fallar a propósito.
public sealed class RecordingUnitOfWork(bool fail = false) : IDocumentsUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (fail)
            throw new InvalidOperationException("Fallo simulado al guardar.");

        SaveCount++;
        return Task.CompletedTask;
    }
}
