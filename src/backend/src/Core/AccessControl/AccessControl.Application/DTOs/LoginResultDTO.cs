namespace AccessControl.Application.Abstractions;

public sealed record LoginResultDTO(string AccessToken, DateTime ExpiresAt);
