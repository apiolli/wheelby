using System.Security.Claims;
using AccessControl.Application.Abstractions;
using AccessControl.Contracts;
using AccessControl.Domain.Sessions.Exceptions;
using Microsoft.AspNetCore.Http;

namespace AccessControl.Infrastructure.Security;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser, ICurrentSession
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public Guid Id => ReadGuid(AuthClaims.Subject);
    public Guid SessionId => ReadGuid(AuthClaims.SessionId);
    public string Email => Read(AuthClaims.Email);
    public string Role => Read(ClaimTypes.Role);

    private string Read(string claimType)
        => (IsAuthenticated ? Principal!.FindFirstValue(claimType) : null)
           ?? throw new InvalidSessionException();

    private Guid ReadGuid(string claimType)
        => Guid.TryParse(Read(claimType), out var value) ? value : throw new InvalidSessionException();
}