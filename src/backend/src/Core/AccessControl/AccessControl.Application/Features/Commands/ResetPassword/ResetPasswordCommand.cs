using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword) : ICommand;
