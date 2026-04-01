using InputAwareDisplaySwitcher.Core.Domain.Devices;

namespace InputAwareDisplaySwitcher.Core.Application;

public delegate Task InputActivityObservedHandler(RuntimeDeviceObservation observation, CancellationToken cancellationToken);

public interface IInputActivitySource
{
    event InputActivityObservedHandler? ActivityObserved;
}
