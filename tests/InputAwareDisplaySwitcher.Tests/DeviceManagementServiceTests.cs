using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Zones;

namespace InputAwareDisplaySwitcher.Tests;

public sealed class DeviceManagementServiceTests
{
    private readonly DeviceManagementService _service = new();

    [Fact]
    public void BuildEntries_ProjectsDetectedPersistedDeviceWithAssignedZone()
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
                }
            ],
            Zones =
            [
                new ZoneDefinition
                {
                    ZoneId = "desk",
                    Name = "Desk",
                    PreferredDisplayProfileId = "desk-profile",
                    Priority = 10
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

        var entries = _service.BuildEntries(snapshot,
        [
            new RuntimeDeviceObservation
            {
                SessionDeviceId = "raw:0001",
                DeviceKind = DeviceKind.Keyboard,
                InstanceId = "desk-keyboard",
                FriendlyName = "USB Keyboard",
                ObservedAtUtc = DateTimeOffset.UtcNow,
                LastSeenAtUtc = DateTimeOffset.UtcNow
            }
        ]);

        var entry = Assert.Single(entries);
        Assert.True(entry.IsDetectedThisSession);
        Assert.Equal("Desk Keyboard", entry.DisplayName);
        Assert.Equal("desk", entry.AssignedZoneId);
        Assert.Equal("Desk", entry.AssignedZoneName);
        Assert.Equal(DeviceAssignmentState.Assigned, entry.AssignmentState);
        Assert.True(entry.CanPersistEdits);
    }

    [Fact]
    public void BuildEntries_SurfacesUnassignedPersistedDevicesClearly()
    {
        var snapshot = new DeviceRegistrySnapshot
        {
            Devices =
            [
                new PersistedDeviceIdentity
                {
                    DeviceId = "mouse-1",
                    FriendlyName = "Travel Mouse",
                    DeviceKind = DeviceKind.Mouse,
                    PreferredPersistenceKey = "instance:travel-mouse"
                }
            ]
        };

        var entry = Assert.Single(_service.BuildEntries(snapshot, []));

        Assert.False(entry.IsDetectedThisSession);
        Assert.Equal(DeviceAssignmentState.Unassigned, entry.AssignmentState);
        Assert.Null(entry.AssignedZoneId);
        Assert.Equal("instance:travel-mouse", entry.StableIdentitySummary);
    }

    [Fact]
    public void ApplyEdits_UpdatesFriendlyNameAndZoneForPersistedDevice()
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
                    PreferredPersistenceKey = "instance:desk-keyboard"
                }
            ]
        };
        var entry = Assert.Single(_service.BuildEntries(snapshot, []));

        var updated = _service.ApplyEdits(
            snapshot,
            entry,
            "Living Room Keyboard",
            "living-room",
            DateTimeOffset.UtcNow);

        var device = Assert.Single(updated.Devices);
        Assert.Equal("Living Room Keyboard", device.FriendlyName);
        Assert.Equal("living-room", device.AssignedZoneId);
    }

    [Fact]
    public void ApplyEdits_CreatesPersistedMappingForNewObservedDevice()
    {
        var snapshot = new DeviceRegistrySnapshot();
        var observedEntry = Assert.Single(_service.BuildEntries(snapshot,
        [
            new RuntimeDeviceObservation
            {
                SessionDeviceId = "raw:0002",
                DeviceKind = DeviceKind.Keyboard,
                InstanceId = "new-keyboard",
                FriendlyName = "Wireless Keyboard",
                ObservedAtUtc = DateTimeOffset.UtcNow
            }
        ]));

        var updated = _service.ApplyEdits(
            snapshot,
            observedEntry,
            "Living Room Keyboard",
            "living-room",
            DateTimeOffset.UtcNow);

        var device = Assert.Single(updated.Devices);
        Assert.Equal("Living Room Keyboard", device.FriendlyName);
        Assert.Equal("living-room", device.AssignedZoneId);
        Assert.Equal("instance:new-keyboard", device.PreferredPersistenceKey);
        Assert.StartsWith("keyboard-", device.DeviceId, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyEdits_RejectsSessionOnlyDevices()
    {
        var snapshot = new DeviceRegistrySnapshot();
        var entry = Assert.Single(_service.BuildEntries(snapshot,
        [
            new RuntimeDeviceObservation
            {
                SessionDeviceId = "raw:0003",
                DeviceKind = DeviceKind.Mouse,
                FriendlyName = "Unknown Mouse",
                ObservedAtUtc = DateTimeOffset.UtcNow
            }
        ]));

        Assert.False(entry.CanPersistEdits);

        var exception = Assert.Throws<InvalidOperationException>(() => _service.ApplyEdits(
            snapshot,
            entry,
            "Unsafe Mouse",
            null,
            DateTimeOffset.UtcNow));

        Assert.Contains("durable persistence key", exception.Message);
    }
}
