using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Zones;

namespace InputAwareDisplaySwitcher.Tests;

public sealed class DeviceRegistryServiceTests
{
    [Fact]
    public void Resolve_ReturnsZoneForMultipleDevicesMappedToSameZone()
    {
        var snapshot = new DeviceRegistrySnapshot
        {
            Devices =
            [
                new PersistedDeviceIdentity
                {
                    DeviceId = "keyboard-1",
                    FriendlyName = "Desk Keyboard",
                    DeviceKind = DeviceKind.Keyboard,
                    PreferredPersistenceKey = "instance:desk-keyboard",
                    AssignedZoneId = "desk"
                },
                new PersistedDeviceIdentity
                {
                    DeviceId = "mouse-1",
                    FriendlyName = "Desk Mouse",
                    DeviceKind = DeviceKind.Mouse,
                    PreferredPersistenceKey = "instance:desk-mouse",
                    AssignedZoneId = "desk"
                }
            ],
            Zones =
            [
                new ZoneDefinition
                {
                    ZoneId = "desk",
                    Name = "Desk",
                    PreferredDisplayProfileId = "desk-profile"
                }
            ],
            DisplayProfiles =
            [
                new DisplayProfile
                {
                    DisplayProfileId = "desk-profile",
                    Name = "Desk Only",
                    IntentKind = DisplayProfileIntentKind.ExternalOnly
                }
            ]
        };

        var service = new DeviceRegistryService(new InMemoryDeviceRegistryStore(snapshot));
        var resolution = service.Resolve(new RuntimeDeviceObservation
        {
            SessionDeviceId = "mouse-session",
            DeviceKind = DeviceKind.Mouse,
            InstanceId = "desk-mouse",
            FriendlyName = "Desk Mouse",
            ObservedAtUtc = DateTimeOffset.UtcNow
        }, snapshot);

        Assert.Equal(DeviceRegistryResolutionStatus.Resolved, resolution.Status);
        Assert.Equal("mouse-1", resolution.MatchedDevice?.DeviceId);
        Assert.Equal("desk", resolution.Zone?.ZoneId);
        Assert.Equal("desk-profile", resolution.TargetProfile?.DisplayProfileId);
    }

    [Fact]
    public void Resolve_HandlesUnknownDeviceGracefully()
    {
        var snapshot = new DeviceRegistrySnapshot();
        var service = new DeviceRegistryService(new InMemoryDeviceRegistryStore(snapshot));

        var resolution = service.Resolve(new RuntimeDeviceObservation
        {
            SessionDeviceId = "unknown-session",
            DeviceKind = DeviceKind.Keyboard,
            InstanceId = "unknown-device",
            FriendlyName = "Unknown Keyboard",
            ObservedAtUtc = DateTimeOffset.UtcNow
        }, snapshot);

        Assert.Equal(DeviceRegistryResolutionStatus.UnknownDevice, resolution.Status);
        Assert.Null(resolution.MatchedDevice);
        Assert.Contains("No persisted device mapping matched", resolution.Message);
    }
}
