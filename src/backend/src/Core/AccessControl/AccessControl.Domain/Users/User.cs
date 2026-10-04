using AccessControl.Domain.Users.Exceptions;
using SharedKernel.Domain;
using SharedKernel.Exceptions;

namespace AccessControl.Domain.Users;

public sealed class User : AggregateRoot<Guid>
{
    public const int FullNameMinLength = 2;
    public const int FullNameMaxLength = 100;
    public static readonly TimeSpan ActivationTokenLifetime = TimeSpan.FromHours(24);
    public string FullName { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public Role Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public OneTimeToken? ActivationToken { get; private set; }

    public bool IsActive => ActivatedAt is not null;

    // Constructor para EF Core
    private User() : base(Guid.Empty) { }

    private User(Guid id, string fullName, Email email, string passwordHash, DateTime utcNow)
        : base(id)
    {
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        Role = Role.Standard;
        CreatedAt = utcNow;
    }

    public static User Create(string? fullName, Email email, string passwordHash, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var cleanName = fullName?.Trim();
        if (string.IsNullOrEmpty(cleanName))
            throw new InvalidUserDataException("El nombre es obligatorio.");

        if (cleanName.Length < FullNameMinLength || cleanName.Length > FullNameMaxLength)
            throw new InvalidUserDataException(
                $"El nombre debe tener entre {FullNameMinLength} y {FullNameMaxLength} caracteres.");

        return new User(Guid.NewGuid(), cleanName, email, passwordHash, utcNow);
    }

    public void IssueActivationToken(string tokenHash, DateTime utcNow)
    {
        if (IsActive)
            throw new ConflictException("La cuenta ya está activa.");

        ActivationToken = OneTimeToken.Issue(tokenHash, utcNow, ActivationTokenLifetime);
    }

    public void Activate(string tokenHash, DateTime utcNow)
    {
        if (ActivationToken is null || !ActivationToken.IsValidFor(tokenHash, utcNow))
            throw new InvalidActivationTokenException();

        ActivationToken = ActivationToken.MarkAsUsed(utcNow);
        ActivatedAt = utcNow;
    }
}