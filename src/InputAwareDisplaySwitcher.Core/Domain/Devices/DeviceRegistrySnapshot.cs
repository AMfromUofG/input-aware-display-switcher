using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Zones;

namespace InputAwareDisplaySwitcher.Core.Domain.Devices;

public sealed record DeviceRegistrySnapshot
{
    public List<PersistedDeviceIdentity> Devices { get; init; } = [];

    public List<ZoneDefinition> Zones { get; init; } = [];

    public List<DisplayProfile> DisplayProfiles { get; init; } = [];

    public ZoneDefinition? FindZone(string? zoneId)
    {
        return string.IsNullOrWhiteSpace(zoneId)
            ? null
            : Zones.FirstOrDefault(zone => string.Equals(zone.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase));
    }

    public DisplayProfile? FindProfile(string? profileId)
    {
        return string.IsNullOrWhiteSpace(profileId)
            ? null
            : DisplayProfiles.FirstOrDefault(profile => string.Equals(profile.DisplayProfileId, profileId, StringComparison.OrdinalIgnoreCase));
    }
}
