namespace AccessControl.Application.Abstractions;

public interface IActivationLinkBuilder
{
    string Build(Guid userId, string token);
}