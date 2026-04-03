namespace InputAwareDisplaySwitcher.Core.Domain.Switching;

public sealed record ApplicationRuntimeState
{
    public string? CurrentZoneId { get; init; }

    public int CurrentZonePriority { get; init; }

    public string? CurrentDisplayProfileId { get; init; }

    public DateTimeOffset? LastSwitchAtUtc { get; init; }

    public DateTimeOffset? LastInputAtUtc { get; init; }

    public string? LastInputZoneId { get; init; }

    public bool IsManualSwitchingLocked { get; init; }

    public string? LastMatchedDeviceId { get; init; }
}
