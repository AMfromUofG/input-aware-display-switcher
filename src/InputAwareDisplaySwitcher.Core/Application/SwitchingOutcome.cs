using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Core.Application;

public sealed record SwitchingOutcome
{
    public required RuntimeDeviceObservation Observation { get; init; }

    public required DeviceRegistryResolution Resolution { get; init; }

    public required SwitchDecision Decision { get; init; }

    public required SwitchExecutionResult ExecutionResult { get; init; }
}
