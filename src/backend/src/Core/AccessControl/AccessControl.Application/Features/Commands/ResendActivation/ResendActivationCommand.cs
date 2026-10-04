using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.ResendActivation;

public sealed record ResendActivationCommand(string Email) : ICommand;