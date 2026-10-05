using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.RequestPasswordReset;

public sealed record RequestPasswordResetCommand(string Email) : ICommand;
