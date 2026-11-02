using Shared.Application.Abstractions;

namespace Wheelby.Test.TestSupport;

// Reloj fijo. Cada prueba pasa los momentos que necesita; se consumen en orden.
public sealed class FixedClock(params DateTime[] moments) : IClock
{
    private int _next;

    public DateTime UtcNow => moments[_next < moments.Length ? _next++ : moments.Length - 1];
}
