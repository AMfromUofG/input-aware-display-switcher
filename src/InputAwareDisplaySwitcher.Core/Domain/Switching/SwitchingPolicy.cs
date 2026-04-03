namespace InputAwareDisplaySwitcher.Core.Domain.Switching;

public sealed record SwitchingPolicy
{
    public bool AutomationEnabled { get; init; } = true;

    public TimeSpan Cooldown { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan RecentActivityThreshold { get; init; } = TimeSpan.FromSeconds(15);

    public PriorityMode PriorityMode { get; init; } = PriorityMode.MostRecentInputWins;

    public bool ManualLockStopsSwitching { get; init; } = true;

    public bool AllowSameProfileRefresh { get; init; }
}
