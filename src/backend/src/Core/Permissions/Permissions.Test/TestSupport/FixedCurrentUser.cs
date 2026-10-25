using AccessControl.Contracts;

namespace Permissions.Test.Support;

// Doble configurable del usuario autenticado (ICurrentUser de AccessControl).
public sealed class FixedCurrentUser : ICurrentUser
{
    public FixedCurrentUser(Guid id, string role = AccessControlRoles.Administrator)
    {
        Id = id;
        Role = role;
    }

    public bool IsAuthenticated => true;

    public Guid Id { get; }

    public string Email { get; } = "usuario@wheelby.dev";

    public string Role { get; }
}
