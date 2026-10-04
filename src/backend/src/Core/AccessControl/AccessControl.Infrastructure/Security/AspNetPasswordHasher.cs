using AccessControl.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AccessControl.Infrastructure.Security;

internal sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    // Hash válido de una contraseña al azar, solo para igualar tiempos de respuesta.
    private readonly string _dummyHash;
    public AspNetPasswordHasher()
        => _dummyHash = _hasher.HashPassword(null!, Guid.NewGuid().ToString("N"));

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password)
        => _hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;

    public void SimulateVerification(string password)
        => _hasher.VerifyHashedPassword(null!, _dummyHash, password);
}