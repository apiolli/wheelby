namespace AccessControl.Contracts;

// Administradores activos para otros módulos (p. ej. notificar una solicitud de permiso).
// Solo lectura. Activo = rol Administrator, cuenta activada y no deshabilitada.
// Va aparte de IUserDirectory para no obligar a cambiar a quienes ya lo implementan.
public interface IAdministratorDirectory
{
    Task<IReadOnlyList<UserContact>> ListActiveAdministratorsAsync(CancellationToken cancellationToken = default);
}
