namespace InputAwareDisplaySwitcher.Core.Domain.Switching;

public sealed record SwitchingPolicy
{
    public TimeSpan Cooldown { get; init; } = TimeSpan.FromSeconds(30);

    public bool ManualLockStopsSwitching { get; init; } = true;

    public bool AllowSameProfileRefresh { get; init; }
}
