using AccessControl.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AccessControl.Infrastructure.Security;

internal sealed class ActivationLinkBuilder(IOptions<AppOptions> options) : IActivationLinkBuilder
{
    public string Build(Guid userId, string token)
        => $"{options.Value.ActivationUrl}?userId={userId}&token={Uri.EscapeDataString(token)}";
}