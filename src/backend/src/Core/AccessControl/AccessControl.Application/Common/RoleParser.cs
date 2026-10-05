using AccessControl.Contracts;
using AccessControl.Domain.Users;

namespace AccessControl.Application.Features.ChangeRole;

internal static class RoleParser
{
    public static bool TryParse(string? value, out Role role)
    {
        if (string.Equals(value, AccessControlRoles.Administrator, StringComparison.OrdinalIgnoreCase))
        {
            role = Role.Administrator;
            return true;
        }

        if (string.Equals(value, AccessControlRoles.Standard, StringComparison.OrdinalIgnoreCase))
        {
            role = Role.Standard;
            return true;
        }

        role = default;
        return false;
    }
}
