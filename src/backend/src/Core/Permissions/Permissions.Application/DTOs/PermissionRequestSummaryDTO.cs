namespace Permissions.Application.DTOs;

public sealed record PermissionRequestSummaryDTO(
    Guid Id,
    Guid RequesterId,
    string PermissionCode,
    string Status,
    Guid? ApproverId,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    string? RejectionReason);
