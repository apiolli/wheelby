using Shared.Application.Abstractions;

namespace Permissions.Application.Features.ExecuteDemoAction;

public sealed record ExecuteDemoActionCommand : ICommand<DemoActionResult>;
