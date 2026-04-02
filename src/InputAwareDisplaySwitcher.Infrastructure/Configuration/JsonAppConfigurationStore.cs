using System.Text.Json;
using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Configuration;
using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;
using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Infrastructure.Configuration;

public sealed class JsonAppConfigurationStore : IAppConfigurationStore
{
    private readonly string _filePath;
    private readonly IDiagnosticsService _diagnostics;

    public JsonAppConfigurationStore(string filePath, IDiagnosticsService? diagnostics = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = filePath;
        _diagnostics = diagnostics ?? NullDiagnosticsService.Instance;
    }

    public async Task<AppConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            var defaultConfiguration = new AppConfiguration();

            _diagnostics.Record(
                DiagnosticCategories.Configuration,
                DiagnosticEventTypes.ConfigurationMissingFile,
                "Configuration file was not found. Using default configuration.",
                details: CreateConfigurationDetails(defaultConfiguration));

            return defaultConfiguration;
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var configuration = await JsonSerializer
                .DeserializeAsync<AppConfiguration>(stream, ConfigurationJsonSerializerOptions.Default, cancellationToken)
                .ConfigureAwait(false);

            var sanitized = Sanitize(configuration);

            _diagnostics.Record(
                DiagnosticCategories.Configuration,
                DiagnosticEventTypes.ConfigurationLoaded,
                "Configuration loaded successfully.",
                details: CreateConfigurationDetails(sanitized));

            return sanitized;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            var defaultConfiguration = new AppConfiguration();

            _diagnostics.Record(
                DiagnosticCategories.Configuration,
                DiagnosticEventTypes.ConfigurationLoadFailed,
                "Configuration could not be loaded. Using default configuration instead.",
                DiagnosticSeverity.Warning,
                CreateFailureDetails(exception, defaultConfiguration));

            return defaultConfiguration;
        }
    }

    public async Task SaveAsync(AppConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var sanitized = Sanitize(configuration);

        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(_filePath);
            await JsonSerializer
                .SerializeAsync(stream, sanitized, ConfigurationJsonSerializerOptions.Default, cancellationToken)
                .ConfigureAwait(false);

            _diagnostics.Record(
                DiagnosticCategories.Configuration,
                DiagnosticEventTypes.ConfigurationSaved,
                "Configuration saved successfully.",
                details: CreateConfigurationDetails(sanitized));
        }
        catch (Exception exception) when (exception is NotSupportedException or IOException or UnauthorizedAccessException)
        {
            _diagnostics.Record(
                DiagnosticCategories.Configuration,
                DiagnosticEventTypes.ConfigurationSaveFailed,
                "Configuration save failed.",
                DiagnosticSeverity.Error,
                CreateFailureDetails(exception, sanitized));

            throw;
        }
    }

    private static AppConfiguration Sanitize(AppConfiguration? configuration)
    {
        if (configuration is null)
        {
            return new AppConfiguration();
        }

        return configuration with
        {
            Version = configuration.Version <= 0 ? AppConfiguration.CurrentVersion : configuration.Version,
            DeviceRegistry = configuration.DeviceRegistry ?? new DeviceRegistrySnapshot(),
            SwitchingPolicy = configuration.SwitchingPolicy ?? new SwitchingPolicy(),
            Preferences = configuration.Preferences ?? new AppPreferences()
        };
    }

    private IReadOnlyDictionary<string, string?> CreateConfigurationDetails(AppConfiguration configuration)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["filePath"] = _filePath,
            ["version"] = configuration.Version.ToString(),
            ["deviceCount"] = configuration.DeviceRegistry.Devices.Count.ToString(),
            ["zoneCount"] = configuration.DeviceRegistry.Zones.Count.ToString(),
            ["profileCount"] = configuration.DeviceRegistry.DisplayProfiles.Count.ToString(),
            ["cooldown"] = configuration.SwitchingPolicy.Cooldown.ToString(),
            ["manualLockStopsSwitching"] = configuration.SwitchingPolicy.ManualLockStopsSwitching.ToString(),
            ["allowSameProfileRefresh"] = configuration.SwitchingPolicy.AllowSameProfileRefresh.ToString(),
            ["isManualSwitchingLocked"] = configuration.Preferences.IsManualSwitchingLocked.ToString()
        };
    }

    private IReadOnlyDictionary<string, string?> CreateFailureDetails(Exception exception, AppConfiguration configuration)
    {
        var details = new Dictionary<string, string?>(CreateConfigurationDetails(configuration), StringComparer.OrdinalIgnoreCase)
        {
            ["exceptionType"] = exception.GetType().Name,
            ["exceptionMessage"] = exception.Message
        };

        return details;
    }
}
