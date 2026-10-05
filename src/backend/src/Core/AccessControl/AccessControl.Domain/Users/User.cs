using AccessControl.Domain.Users.Exceptions;
using SharedKernel.Domain;
using SharedKernel.Exceptions;

namespace AccessControl.Domain.Users;

public sealed class User : AggregateRoot<Guid>
{
    public const int FullNameMinLength = 2;
    public const int FullNameMaxLength = 100;
    public const int MaxFailedLoginAttempts = 5;
    public string FullName { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public Role Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? DisabledAt { get; private set; }
    public OneTimeToken? ActivationToken { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }

    public bool IsActive => ActivatedAt is not null;
    public bool IsDisabled => DisabledAt is not null;
    public static readonly TimeSpan ActivationTokenLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

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

    public static User CreateAdministrator(string? fullName, Email email, string passwordHash, DateTime utcNow)
    {
        var user = Create(fullName, email, passwordHash, utcNow);
        user.Role = Role.Administrator;
        user.ActivatedAt = utcNow;
        return user;
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

    // Inicio de sesión

    public bool IsLocked(DateTime utcNow) => LockedUntil is not null && utcNow < LockedUntil;

    public void EnsureNotLocked(DateTime utcNow)
    {
        if (IsLocked(utcNow))
            throw new AccountLockedException();
    }

    public void EnsureCanSignIn()
    {
        if (IsDisabled)
            throw new AccountDisabledException();

        if (!IsActive)
            throw new AccountNotActiveException();
    }

    public void RegisterFailedLogin(DateTime utcNow)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            LockedUntil = utcNow.Add(LockoutDuration);
            FailedLoginAttempts = 0;
        }
    }

    public void RegisterSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    // --- Administración ---
    public void ChangeRole(Role newRole)
    {
        if (!Enum.IsDefined(newRole))
            throw new InvalidUserDataException("El rol no es válido.");

        Role = newRole;
    }

    public void Disable(Guid actorId, DateTime utcNow)
    {
        if (actorId == Id)
            throw new CannotDisableSelfException();

        DisabledAt ??= utcNow;
    }

    public void Enable() => DisabledAt = null;

    // Red de seguridad del seed: deja al Administrador fijo con rol, activo y habilitado.
    // No toca su contraseña.
    public void RestoreAdministratorAccess(DateTime utcNow)
    {
        Role = Role.Administrator;
        DisabledAt = null;
        ActivatedAt ??= utcNow;
    }
}