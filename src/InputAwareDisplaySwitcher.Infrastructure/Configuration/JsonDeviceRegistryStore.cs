using System.Text.Json;
using System.Text.Json.Serialization;
using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Devices;

namespace InputAwareDisplaySwitcher.Infrastructure.Configuration;

public sealed class JsonDeviceRegistryStore : IDeviceRegistryStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public JsonDeviceRegistryStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public async Task<DeviceRegistrySnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return new DeviceRegistrySnapshot();
        }

        await using var stream = File.OpenRead(_filePath);
        var snapshot = await JsonSerializer
            .DeserializeAsync<DeviceRegistrySnapshot>(stream, _serializerOptions, cancellationToken)
            .ConfigureAwait(false);

        return snapshot ?? new DeviceRegistrySnapshot();
    }

    public async Task SaveAsync(DeviceRegistrySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        await JsonSerializer
            .SerializeAsync(stream, snapshot, _serializerOptions, cancellationToken)
            .ConfigureAwait(false);
    }
}
