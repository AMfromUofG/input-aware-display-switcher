using InputAwareDisplaySwitcher.Core.Domain.Configuration;
using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;
using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Switching;
using InputAwareDisplaySwitcher.Core.Domain.Zones;
using InputAwareDisplaySwitcher.Infrastructure.Configuration;
using InputAwareDisplaySwitcher.Infrastructure.Diagnostics;

namespace InputAwareDisplaySwitcher.Tests;

public sealed class JsonAppConfigurationStoreTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"iads-config-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task LoadAsync_WhenFileIsMissing_ReturnsDefaultConfiguration()
    {
        var diagnostics = new DiagnosticsService();
        var filePath = Path.Combine(_tempDirectory, "config.json");
        var store = new JsonAppConfigurationStore(filePath, diagnostics);

        var configuration = await store.LoadAsync();

        Assert.Equal(AppConfiguration.CurrentVersion, configuration.Version);
        Assert.Empty(configuration.DeviceRegistry.Devices);
        Assert.Empty(configuration.DeviceRegistry.Zones);
        Assert.Empty(configuration.DeviceRegistry.DisplayProfiles);
        Assert.True(configuration.SwitchingPolicy.AutomationEnabled);
        Assert.Equal(TimeSpan.FromSeconds(30), configuration.SwitchingPolicy.Cooldown);
        Assert.Equal(TimeSpan.FromSeconds(15), configuration.SwitchingPolicy.RecentActivityThreshold);
        Assert.Equal(PriorityMode.MostRecentInputWins, configuration.SwitchingPolicy.PriorityMode);
        Assert.False(configuration.Preferences.IsManualSwitchingLocked);
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.ConfigurationMissingFile);
    }

    [Fact]
    public async Task LoadAsync_WhenJsonIsMalformed_ReturnsDefaultsGracefully()
    {
        Directory.CreateDirectory(_tempDirectory);
        var filePath = Path.Combine(_tempDirectory, "config.json");
        await File.WriteAllTextAsync(filePath, "{ this is not valid json");

        var diagnostics = new DiagnosticsService();
        var store = new JsonAppConfigurationStore(filePath, diagnostics);

        var configuration = await store.LoadAsync();

        Assert.Equal(AppConfiguration.CurrentVersion, configuration.Version);
        Assert.Empty(configuration.DeviceRegistry.Devices);
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.ConfigurationLoadFailed);
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_RoundTripsConfigurationData()
    {
        Directory.CreateDirectory(_tempDirectory);
        var filePath = Path.Combine(_tempDirectory, "config.json");
        var diagnostics = new DiagnosticsService();
        var store = new JsonAppConfigurationStore(filePath, diagnostics);
        var configuration = new AppConfiguration
        {
            DeviceRegistry = new DeviceRegistrySnapshot
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
            },
            SwitchingPolicy = new SwitchingPolicy
            {
                AutomationEnabled = false,
                Cooldown = TimeSpan.FromMinutes(2),
                RecentActivityThreshold = TimeSpan.FromSeconds(20),
                PriorityMode = PriorityMode.PreferHigherPriorityZone,
                ManualLockStopsSwitching = true,
                AllowSameProfileRefresh = true
            },
            Preferences = new AppPreferences
            {
                IsManualSwitchingLocked = true
            }
        };

        await store.SaveAsync(configuration);
        var loaded = await store.LoadAsync();

        Assert.Single(loaded.DeviceRegistry.Devices);
        Assert.Single(loaded.DeviceRegistry.Zones);
        Assert.Single(loaded.DeviceRegistry.DisplayProfiles);
        Assert.False(loaded.SwitchingPolicy.AutomationEnabled);
        Assert.Equal(TimeSpan.FromMinutes(2), loaded.SwitchingPolicy.Cooldown);
        Assert.Equal(TimeSpan.FromSeconds(20), loaded.SwitchingPolicy.RecentActivityThreshold);
        Assert.Equal(PriorityMode.PreferHigherPriorityZone, loaded.SwitchingPolicy.PriorityMode);
        Assert.True(loaded.SwitchingPolicy.AllowSameProfileRefresh);
        Assert.True(loaded.Preferences.IsManualSwitchingLocked);
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.ConfigurationSaved);
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.ConfigurationLoaded);
    }

    [Fact]
    public async Task LoadAsync_WhenSectionsAreNull_UsesSafeDefaults()
    {
        Directory.CreateDirectory(_tempDirectory);
        var filePath = Path.Combine(_tempDirectory, "config.json");
        await File.WriteAllTextAsync(filePath, """
            {
              "version": 1,
              "deviceRegistry": null,
              "switchingPolicy": null,
              "preferences": null
            }
            """);

        var store = new JsonAppConfigurationStore(filePath, new DiagnosticsService());

        var configuration = await store.LoadAsync();

        Assert.NotNull(configuration.DeviceRegistry);
        Assert.NotNull(configuration.SwitchingPolicy);
        Assert.NotNull(configuration.Preferences);
        Assert.True(configuration.SwitchingPolicy.AutomationEnabled);
        Assert.Equal(TimeSpan.FromSeconds(30), configuration.SwitchingPolicy.Cooldown);
        Assert.Equal(TimeSpan.FromSeconds(15), configuration.SwitchingPolicy.RecentActivityThreshold);
        Assert.Equal(PriorityMode.MostRecentInputWins, configuration.SwitchingPolicy.PriorityMode);
        Assert.False(configuration.Preferences.IsManualSwitchingLocked);
    }

    [Fact]
    public async Task LoadAsync_WhenOptionalSectionsAreOmitted_UsesSafeDefaults()
    {
        Directory.CreateDirectory(_tempDirectory);
        var filePath = Path.Combine(_tempDirectory, "config.json");
        await File.WriteAllTextAsync(filePath, """
            {
              "version": 1,
              "deviceRegistry": {
                "devices": [],
                "zones": [],
                "displayProfiles": []
              }
            }
            """);

        var store = new JsonAppConfigurationStore(filePath, new DiagnosticsService());

        var configuration = await store.LoadAsync();

        Assert.Equal(TimeSpan.FromSeconds(30), configuration.SwitchingPolicy.Cooldown);
        Assert.Equal(TimeSpan.FromSeconds(15), configuration.SwitchingPolicy.RecentActivityThreshold);
        Assert.Equal(PriorityMode.MostRecentInputWins, configuration.SwitchingPolicy.PriorityMode);
        Assert.False(configuration.Preferences.IsManualSwitchingLocked);
    }

    [Fact]
    public async Task LoadAsync_WhenDurationsAreNegative_UsesSafeDefaults()
    {
        Directory.CreateDirectory(_tempDirectory);
        var filePath = Path.Combine(_tempDirectory, "config.json");
        await File.WriteAllTextAsync(filePath, """
            {
              "version": 1,
              "deviceRegistry": {
                "devices": null,
                "zones": null,
                "displayProfiles": null
              },
              "switchingPolicy": {
                "automationEnabled": false,
                "cooldown": "-00:00:05",
                "recentActivityThreshold": "-00:00:03",
                "priorityMode": "PreferHigherPriorityZone"
              }
            }
            """);

        var store = new JsonAppConfigurationStore(filePath, new DiagnosticsService());

        var configuration = await store.LoadAsync();

        Assert.Empty(configuration.DeviceRegistry.Devices);
        Assert.Empty(configuration.DeviceRegistry.Zones);
        Assert.Empty(configuration.DeviceRegistry.DisplayProfiles);
        Assert.False(configuration.SwitchingPolicy.AutomationEnabled);
        Assert.Equal(TimeSpan.FromSeconds(30), configuration.SwitchingPolicy.Cooldown);
        Assert.Equal(TimeSpan.FromSeconds(15), configuration.SwitchingPolicy.RecentActivityThreshold);
        Assert.Equal(PriorityMode.PreferHigherPriorityZone, configuration.SwitchingPolicy.PriorityMode);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
