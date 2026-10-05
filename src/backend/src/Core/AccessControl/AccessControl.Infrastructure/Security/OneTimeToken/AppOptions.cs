namespace AccessControl.Infrastructure.Security;

internal sealed class AppOptions
{
    public const string SectionName = "App";
    public string ActivationUrl { get; init; } = string.Empty;
}