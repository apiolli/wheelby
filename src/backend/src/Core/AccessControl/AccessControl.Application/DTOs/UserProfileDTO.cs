namespace AccessControl.Application.Abstractions;

public sealed record UserProfileDTO(Guid Id, string FullName, string Email, string Role);
