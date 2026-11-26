using Shared.Application.Abstractions;

namespace Notifications.Test.TestSupport;

// RD-11/RD-12: reloj fijo. Cada prueba decide la fecha.
public sealed class FixedClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; set; } = utcNow;
}
