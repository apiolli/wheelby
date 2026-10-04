namespace AccessControl.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
}