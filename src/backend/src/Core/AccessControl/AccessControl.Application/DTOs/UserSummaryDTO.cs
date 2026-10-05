namespace AccessControl.Application.DTOs;

public sealed record UserSummaryDTO(
    Guid Id, string FullName, string Email, string Role, string Status, DateTime CreatedAt);