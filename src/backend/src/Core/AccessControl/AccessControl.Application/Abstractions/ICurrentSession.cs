namespace AccessControl.Application.Abstractions;

public interface ICurrentSession
{
    Guid SessionId { get; }
}