using System.Text.RegularExpressions;

namespace Permissions.Domain.PermissionRequests;

// Formato del código de permiso: minúsculas, dígitos, puntos y guiones.
// El código es texto opaco: solo se valida la forma, no su significado.
public static class PermissionCodeRules
{
    public const int MaxLength = 100;

    private static readonly Regex Pattern = new(
        "^[a-z0-9]+([.-][a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValid(string? code)
        => code is not null
           && code.Length is > 0 and <= MaxLength
           && Pattern.IsMatch(code);
}
