using InputAwareDisplaySwitcher.Core.Domain.Devices;

namespace InputAwareDisplaySwitcher.Core.Application;

public interface IInputDeviceSnapshotProvider
{
    Task<IReadOnlyList<RuntimeDeviceObservation>> GetCurrentDevicesAsync(CancellationToken cancellationToken = default);
}
