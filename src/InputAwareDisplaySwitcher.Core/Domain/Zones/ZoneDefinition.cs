namespace InputAwareDisplaySwitcher.Core.Domain.Zones;

public sealed record ZoneDefinition
{
    public required string ZoneId { get; init; }

    public required string Name { get; init; }

    public required string PreferredDisplayProfileId { get; init; }

    public int Priority { get; init; }

    public bool IsEnabled { get; init; } = true;

    public string? Description { get; init; }
}
