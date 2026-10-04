using SharedKernel.Domain;

namespace AccessControl.Domain.Sessions;

public sealed class Session : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private Session() : base(Guid.Empty) { }

    private Session(Guid id, Guid userId, DateTime utcNow, DateTime expiresAt) : base(id)
    {
        UserId = userId;
        CreatedAt = utcNow;
        ExpiresAt = expiresAt;
    }

    public static Session Start(Guid userId, DateTime utcNow, TimeSpan lifetime)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("El usuario de la sesión es obligatorio.", nameof(userId));
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime));

        return new Session(Guid.NewGuid(), userId, utcNow, utcNow.Add(lifetime));
    }

    public bool IsValid(DateTime utcNow) => RevokedAt is null && utcNow < ExpiresAt;

    // Cerrar una sesión ya cerrada no es un error, queda como estaba
    public void Revoke(DateTime utcNow) => RevokedAt ??= utcNow;
}