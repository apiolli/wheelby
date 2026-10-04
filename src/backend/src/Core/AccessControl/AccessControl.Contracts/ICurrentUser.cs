namespace AccessControl.Contracts;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid Id { get; }
    string Email { get; }
    string Role { get; } 
}