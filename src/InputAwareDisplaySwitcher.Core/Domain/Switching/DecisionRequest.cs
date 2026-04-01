using InputAwareDisplaySwitcher.Core.Domain.Devices;

namespace InputAwareDisplaySwitcher.Core.Domain.Switching;

public sealed record DecisionRequest
{
    public required RuntimeDeviceObservation Observation { get; init; }

    public required DeviceRegistryResolution Resolution { get; init; }

    public required ApplicationRuntimeState RuntimeState { get; init; }

    public required SwitchingPolicy Policy { get; init; }

    public DateTimeOffset EvaluatedAtUtc { get; init; }
}
