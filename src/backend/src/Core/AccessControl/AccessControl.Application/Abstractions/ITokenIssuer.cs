namespace AccessControl.Application.Abstractions;

public sealed record IssuedToken(string Value, DateTime ExpiresAt);

public interface ITokenIssuer
{
    TimeSpan Lifetime { get; }
    IssuedToken Issue(Guid userId, Guid sessionId, DateTime issuedAt, DateTime expiresAt);
}