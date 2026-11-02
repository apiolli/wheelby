using AccessControl.Contracts;

namespace Wheelby.Test.TestSupport;

// Doble mutable del usuario autenticado. Permite cambiar de usuario dentro de una prueba.
public sealed class MutableCurrentUser : ICurrentUser
{
    public MutableCurrentUser(Guid id, string role = AccessControlRoles.Standard)
    {
        Id = id;
        Role = role;
    }

    public bool IsAuthenticated => true;
    public Guid Id { get; set; }
    public string Email { get; set; } = "usuario@wheelby.dev";
    public string Role { get; set; }
}

internal static class AccessControlRoles
{
    public const string Standard = "Standard";
    public const string Administrator = "Administrator";
}
