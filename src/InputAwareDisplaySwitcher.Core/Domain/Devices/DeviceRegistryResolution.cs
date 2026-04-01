using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Zones;

namespace InputAwareDisplaySwitcher.Core.Domain.Devices;

public sealed record DeviceRegistryResolution
{
    public required DeviceRegistryResolutionStatus Status { get; init; }

    public required RuntimeDeviceObservation Observation { get; init; }

    public PersistedDeviceIdentity? MatchedDevice { get; init; }

    public ZoneDefinition? Zone { get; init; }

    public DisplayProfile? TargetProfile { get; init; }

    public string? MatchedByPersistenceKey { get; init; }

    public string? Message { get; init; }

    public bool HasResolvedTarget => Status == DeviceRegistryResolutionStatus.Resolved;
}
