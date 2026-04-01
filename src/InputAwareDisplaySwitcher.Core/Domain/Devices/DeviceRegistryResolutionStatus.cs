namespace InputAwareDisplaySwitcher.Core.Domain.Devices;

public enum DeviceRegistryResolutionStatus
{
    UnknownDevice = 0,
    DeviceDisabled = 1,
    UnmappedZone = 2,
    ZoneDisabled = 3,
    ProfileUnavailable = 4,
    Resolved = 5
}
