using AccessControl.Application.Common;
using AccessControl.Application.DTOs;

namespace AccessControl.Application.Abstractions;


public static class UserStatus
{
    public const string PendingActivation = "PendingActivation";
    public const string Active = "Active";
    public const string Disabled = "Disabled";                   
}

public interface IUserReadStore
{
    Task<UserProfileDTO?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);

    Task<PagedResult<UserSummaryDTO>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}