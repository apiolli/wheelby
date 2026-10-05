using AccessControl.Application.Abstractions;
using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<LoginResultDTO>;
