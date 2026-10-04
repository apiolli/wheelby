namespace AccessControl.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
    // Hace el mismo trabajo que Verify contra un hash falso. Se usa cuando el correo no existe,
    // para que esa respuesta no sea más rápida que la de un usuario real.
    void SimulateVerification(string password);
}