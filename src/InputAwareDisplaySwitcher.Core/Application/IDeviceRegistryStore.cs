using InputAwareDisplaySwitcher.Core.Domain.Devices;

namespace InputAwareDisplaySwitcher.Core.Application;

public interface IDeviceRegistryStore
{
    Task<DeviceRegistrySnapshot> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(DeviceRegistrySnapshot snapshot, CancellationToken cancellationToken = default);
}
