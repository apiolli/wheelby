using System.Net.Mail;
using AccessControl.Domain.Users.Exceptions;

namespace AccessControl.Domain.Users;

public sealed record Email
{
    public const int MaxLength = 254;

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string? value)
    {
        // Normalizar: recortar y pasar a minúsculas
        var normalized = value?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(normalized))
            throw new InvalidUserDataException("El correo es obligatorio.");

        if (normalized.Length > MaxLength)
            throw new InvalidUserDataException("El correo es demasiado largo.");

        if (!MailAddress.TryCreate(normalized, out var parsed) || parsed.Address != normalized)
            throw new InvalidUserDataException("El correo no tiene un formato válido.");

        return new Email(normalized);
    }

    public override string ToString() => Value;
}