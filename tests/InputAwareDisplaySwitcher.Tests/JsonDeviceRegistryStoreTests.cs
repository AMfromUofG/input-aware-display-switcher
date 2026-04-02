using InputAwareDisplaySwitcher.Core.Domain.Configuration;
using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Switching;
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

    [Fact]
    public async Task SaveAsync_PreservesPolicyAndPreferencesInRootConfiguration()
    {
        Directory.CreateDirectory(_tempDirectory);
        var filePath = Path.Combine(_tempDirectory, "config.json");
        var configurationStore = new JsonAppConfigurationStore(filePath);
        await configurationStore.SaveAsync(new AppConfiguration
        {
            SwitchingPolicy = new SwitchingPolicy
            {
                Cooldown = TimeSpan.FromMinutes(5),
                AllowSameProfileRefresh = true
            },
            Preferences = new AppPreferences
            {
                IsManualSwitchingLocked = true
            }
        });

        var registryStore = new JsonDeviceRegistryStore(configurationStore);
        await registryStore.SaveAsync(new DeviceRegistrySnapshot
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
            ]
        });

        var configuration = await configurationStore.LoadAsync();

        Assert.Single(configuration.DeviceRegistry.Devices);
        Assert.Equal(TimeSpan.FromMinutes(5), configuration.SwitchingPolicy.Cooldown);
        Assert.True(configuration.SwitchingPolicy.AllowSameProfileRefresh);
        Assert.True(configuration.Preferences.IsManualSwitchingLocked);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
