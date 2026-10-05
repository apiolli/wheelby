using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.Register;

public sealed record RegisterUserCommand(string FullName, string Email, string Password) : ICommand<Guid>;

