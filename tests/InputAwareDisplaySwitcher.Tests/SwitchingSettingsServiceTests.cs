using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Tests;

public sealed class SwitchingSettingsServiceTests
{
    private readonly SwitchingSettingsService _service = new();

    [Fact]
    public void Validate_RejectsNegativeCooldown()
    {
        var result = _service.Validate(new SwitchingSettingsInput
        {
            CooldownSecondsText = "-1",
            RecentActivityThresholdSecondsText = "15"
        });

        Assert.False(result.IsValid);
        Assert.Equal("Cooldown cannot be negative.", result.CooldownError);
    }

    [Fact]
    public void Validate_RejectsUnsupportedPriorityMode()
    {
        var result = _service.Validate(new SwitchingSettingsInput
        {
            CooldownSecondsText = "30",
            RecentActivityThresholdSecondsText = "15",
            PriorityMode = (PriorityMode)999
        });

        Assert.False(result.IsValid);
        Assert.Equal("Priority behaviour must be one of the supported options.", result.PriorityModeError);
    }

    [Fact]
    public void Validate_BuildsPolicyForValidInputAndPreservesExistingFlags()
    {
        var result = _service.Validate(new SwitchingSettingsInput
        {
            AutomationEnabled = false,
            CooldownSecondsText = "45",
            RecentActivityThresholdSecondsText = "20",
            PriorityMode = PriorityMode.PreferHigherPriorityZone,
            ManualLockStopsSwitching = false,
            AllowSameProfileRefresh = true
        });

        Assert.True(result.IsValid);
        Assert.NotNull(result.Policy);
        Assert.False(result.Policy.AutomationEnabled);
        Assert.Equal(TimeSpan.FromSeconds(45), result.Policy.Cooldown);
        Assert.Equal(TimeSpan.FromSeconds(20), result.Policy.RecentActivityThreshold);
        Assert.Equal(PriorityMode.PreferHigherPriorityZone, result.Policy.PriorityMode);
        Assert.False(result.Policy.ManualLockStopsSwitching);
        Assert.True(result.Policy.AllowSameProfileRefresh);
    }
}
