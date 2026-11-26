using AccessControl.Contracts;

namespace Notifications.Test.TestSupport;

// Doble configurable del usuario autenticado (ICurrentUser de AccessControl).
public sealed class FixedCurrentUser(Guid id, string role = AccessControlRoles.Standard) : ICurrentUser
{
    public bool IsAuthenticated => true;
    public Guid Id { get; } = id;
    public string Email { get; } = "usuario@wheelby.dev";
    public string Role { get; } = role;
}
