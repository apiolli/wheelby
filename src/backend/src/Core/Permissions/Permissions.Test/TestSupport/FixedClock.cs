using Shared.Application.Abstractions;

namespace Permissions.Test.Support;

// RD-12: reloj fijo con fecha fija. Cada prueba decide las fechas que pasa.
public sealed class FixedClock(params DateTime[] moments) : IClock
{
    private int _next;

    public DateTime UtcNow => moments[_next < moments.Length ? _next++ : moments.Length - 1];
}
