using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Zones;
using InputAwareDisplaySwitcher.Infrastructure.Configuration;

namespace InputAwareDisplaySwitcher.Tests;

public sealed class JsonDeviceRegistryStoreTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"iads-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsRegistryData()
    {
        Directory.CreateDirectory(_tempDirectory);
        var filePath = Path.Combine(_tempDirectory, "registry.json");
        var store = new JsonDeviceRegistryStore(filePath);
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
                    PreferredDisplayProfileId = "desk-profile"
                }
            ],
            DisplayProfiles =
            [
                new DisplayProfile
                {
                    DisplayProfileId = "desk-profile",
                    Name = "Desk Only",
                    IntentKind = DisplayProfileIntentKind.ExternalOnly,
                    ImplementationHints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["windows.topology"] = "external"
                    }
                }
            ]
        };

        await store.SaveAsync(snapshot);
        var loaded = await store.LoadAsync();

        Assert.Single(loaded.Devices);
        Assert.Single(loaded.Zones);
        Assert.Single(loaded.DisplayProfiles);
        Assert.Equal("desk", loaded.Devices[0].AssignedZoneId);
        Assert.Equal("external", loaded.DisplayProfiles[0].ImplementationHints["windows.topology"]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
