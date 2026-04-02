using InputAwareDisplaySwitcher.Core.Domain.Configuration;
using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class RulesSettingsViewModel : SectionViewModelBase
{
    public RulesSettingsViewModel(SwitchingPolicy policy, AppPreferences preferences)
        : base("Rules / Settings", "Switching policy, cooldowns, and operator preferences.")
    {
        Cooldown = policy.Cooldown;
        ManualLockStopsSwitching = policy.ManualLockStopsSwitching;
        AllowSameProfileRefresh = policy.AllowSameProfileRefresh;
        IsManualSwitchingLocked = preferences.IsManualSwitchingLocked;
    }

    public TimeSpan Cooldown { get; }

    public bool ManualLockStopsSwitching { get; }

    public bool AllowSameProfileRefresh { get; }

    public bool IsManualSwitchingLocked { get; }
}
