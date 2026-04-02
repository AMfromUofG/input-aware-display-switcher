using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Core.Domain.Configuration;

public sealed record AppConfiguration
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public DeviceRegistrySnapshot DeviceRegistry { get; init; } = new();

    public SwitchingPolicy SwitchingPolicy { get; init; } = new();

    public AppPreferences Preferences { get; init; } = new();
}
