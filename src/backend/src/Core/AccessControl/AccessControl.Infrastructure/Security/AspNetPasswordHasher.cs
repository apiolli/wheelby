using AccessControl.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AccessControl.Infrastructure.Security;

// PBKDF2 con sal aleatoria incluida en el hash: dos usuarios con la misma contraseña
// no comparten el valor almacenado (RF-CA-02, RD-05).
internal sealed class AspNetPasswordHasher : IPasswordHasher
{
    // El genérico es obligatorio, pero el hasher no usa al usuario.
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);
}