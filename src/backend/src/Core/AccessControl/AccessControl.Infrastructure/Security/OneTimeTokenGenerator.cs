using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using AccessControl.Application.Abstractions;

namespace AccessControl.Infrastructure.Security;

internal sealed class OneTimeTokenGenerator : IOneTimeTokenGenerator
{
    public GeneratedToken Generate()
    {
        // 32 bytes aleatorios (256 bits), seguros para ir en una URL.
        var value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        return new GeneratedToken(value, Hash(value));
    }

    // SHA-256 basta: un token aleatorio largo no se adivina por fuerza bruta.
    public string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}