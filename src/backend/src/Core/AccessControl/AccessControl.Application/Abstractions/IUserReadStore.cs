namespace AccessControl.Application.Abstractions;

public interface IUserReadStore
{
    Task<UserProfileDTO?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
}