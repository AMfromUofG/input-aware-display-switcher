using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Devices;

namespace InputAwareDisplaySwitcher.Infrastructure.Configuration;

public sealed class JsonDeviceRegistryStore : IDeviceRegistryStore
{
    private readonly IAppConfigurationStore _configurationStore;

    public JsonDeviceRegistryStore(string filePath, IDiagnosticsService? diagnostics = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _configurationStore = new JsonAppConfigurationStore(filePath, diagnostics);
    }

    public JsonDeviceRegistryStore(IAppConfigurationStore configurationStore)
    {
        _configurationStore = configurationStore;
    }

    public async Task<DeviceRegistrySnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        var configuration = await _configurationStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        return configuration.DeviceRegistry ?? new DeviceRegistrySnapshot();
    }

    public async Task SaveAsync(DeviceRegistrySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var configuration = await _configurationStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var updatedConfiguration = configuration with
        {
            DeviceRegistry = snapshot
        };

        await _configurationStore.SaveAsync(updatedConfiguration, cancellationToken).ConfigureAwait(false);
    }
}
