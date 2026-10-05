using System.Security.Claims;
using System.Text;
using AccessControl.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AccessControl.Infrastructure.Security;

internal sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly JwtOptions _jwt;
    private readonly JsonWebTokenHandler _handler = new();
    private readonly SigningCredentials _credentials;

    public JwtTokenIssuer(IOptions<JwtOptions> options)
    {
        _jwt = options.Value;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            SecurityAlgorithms.HmacSha256);
    }

    public TimeSpan Lifetime => TimeSpan.FromMinutes(_jwt.ExpirationMinutes);

    public IssuedToken Issue(Guid userId, Guid sessionId, DateTime issuedAt, DateTime expiresAt)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(AuthClaims.Subject, userId.ToString()),
                new Claim(AuthClaims.SessionId, sessionId.ToString())
            }),
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expiresAt,
            SigningCredentials = _credentials
        };

        return new IssuedToken(_handler.CreateToken(descriptor), expiresAt);
    }
}