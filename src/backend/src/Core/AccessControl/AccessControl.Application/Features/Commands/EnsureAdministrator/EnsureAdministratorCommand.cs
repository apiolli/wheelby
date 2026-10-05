using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.EnsureAdministrator;

public sealed record EnsureAdministratorCommand(string FullName, string Email, string Password) : ICommand;
