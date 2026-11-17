namespace AccessControl.Contracts;

// Datos de contacto de un usuario para otros módulos (p. ej. avisar por correo).
// Solo lectura; no expone el rol ni el estado de la cuenta.
public interface IUserDirectory
{
    // null si el usuario no existe o está deshabilitado.
    Task<UserContact?> GetContactAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record UserContact(Guid Id, string FullName, string Email);
