using System.Security.Claims;
using System.Text;
using AccessControl.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Application.Abstractions;

namespace AccessControl.Infrastructure.Security;

internal sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
            return;

        var jwt = jwtOptions.Value;

        options.MapInboundClaims = false; // "sub" y "sid" se quedan con su nombre
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = AuthClaims.Subject,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ValidateSessionAsync,

            // Sin token, o token inválido, vencido o de una sesión cerrada: siempre el mismo rechazo (RD-08).
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.Headers.WWWAuthenticate = "Bearer";
                return ProblemResponse.WriteAsync(
                    context.HttpContext, StatusCodes.Status401Unauthorized,
                    "La sesión no es válida o ha expirado.");
            },

            // Autenticado, pero sin el rol que exige la operación (RF-CA-06).
            OnForbidden = context => ProblemResponse.WriteAsync(
                context.HttpContext, StatusCodes.Status403Forbidden,
                "No tienes permiso para realizar esta operación.")
        };
    }

    public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);

    // La firma y el vencimiento ya están comprobados. Ahora se pregunta a la base si la
    // sesión sigue vigente y el usuario sigue activo, y de ahí sale el rol (no del token).
    private static async Task ValidateSessionAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var subject = principal?.FindFirstValue(AuthClaims.Subject);
        var sessionClaim = principal?.FindFirstValue(AuthClaims.SessionId);

        if (!Guid.TryParse(subject, out var userId) || !Guid.TryParse(sessionClaim, out var sessionId))
        {
            context.Fail("Invalid token claims.");
            return;
        }

        var services = context.HttpContext.RequestServices;
        var db = services.GetRequiredService<AccessControlDbContext>();
        var now = services.GetRequiredService<IClock>().UtcNow;

        var data = await (
                from session in db.Sessions
                join user in db.Users on session.UserId equals user.Id
                where session.Id == sessionId && session.UserId == userId
                select new { session.RevokedAt, session.ExpiresAt, user.ActivatedAt, user.DisabledAt, user.Email, user.Role })
            .AsNoTracking()
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

        if (data is null || data.RevokedAt is not null || now >= data.ExpiresAt
            || data.ActivatedAt is null || data.DisabledAt is not null)
        {
            context.Fail("Session is not valid.");
            return;
        }

        principal!.AddIdentity(new ClaimsIdentity(new[]
        {
            new Claim(AuthClaims.Email, data.Email.Value),
            new Claim(ClaimTypes.Role, RoleMapping.ToContract(data.Role))
        }));
    }
}