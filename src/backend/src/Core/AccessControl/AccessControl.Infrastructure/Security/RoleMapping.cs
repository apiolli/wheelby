using AccessControl.Contracts;
using AccessControl.Domain.Users;

namespace AccessControl.Infrastructure.Security;

// Único lugar donde se convierte el enum del dominio a los textos públicos de Contracts.
internal static class RoleMapping
{
    public static string ToContract(Role role) => role switch
    {
        Role.Administrator => AccessControlRoles.Administrator,
        Role.Standard => AccessControlRoles.Standard,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };
}