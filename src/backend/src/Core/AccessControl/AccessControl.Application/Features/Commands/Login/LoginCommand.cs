using AccessControl.Application.Abstractions;
using Shared.Application.Abstractions;

namespace Accessontrol.Application.Features.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResultDTO>;

