using AccessControl.Contracts;

namespace Documents.Test.TestSupport;

// Doble mutable del usuario autenticado. Los roles se escriben a mano en las pruebas.
public sealed class MutableCurrentUser(Guid id, string role = "Standard") : ICurrentUser
{
    public bool IsAuthenticated => true;
    public Guid Id { get; set; } = id;
    public string Email { get; set; } = "usuario@wheelby.dev";
    public string Role { get; set; } = role;
}
