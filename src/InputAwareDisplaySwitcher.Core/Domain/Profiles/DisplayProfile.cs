namespace InputAwareDisplaySwitcher.Core.Domain.Profiles;

public sealed record DisplayProfile
{
    public required string DisplayProfileId { get; init; }

    public required string Name { get; init; }

    public DisplayProfileIntentKind IntentKind { get; init; } = DisplayProfileIntentKind.Unknown;

    public string? Description { get; init; }

    public Dictionary<string, string> ImplementationHints { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsEnabled { get; init; } = true;
}
