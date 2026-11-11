using Shared.Application.Abstractions;

namespace Documents.Test.TestSupport;

// Reloj fijo: siempre devuelve el mismo momento.
public sealed class FixedClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; } = utcNow;
}
