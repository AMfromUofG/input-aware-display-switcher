namespace InputAwareDisplaySwitcher.Core.Domain.Configuration;

public sealed record AppPreferences
{
    public bool IsManualSwitchingLocked { get; init; }
}
