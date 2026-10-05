using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.ForcePasswordReset;

public sealed record ForcePasswordResetCommand(Guid UserId) : ICommand;
