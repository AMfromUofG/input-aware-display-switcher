using InputAwareDisplaySwitcher.Core.Abstractions;
using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Tests;

internal sealed class InMemoryDeviceRegistryStore : IDeviceRegistryStore
{
    private DeviceRegistrySnapshot _snapshot;

    public InMemoryDeviceRegistryStore(DeviceRegistrySnapshot? snapshot = null)
    {
        _snapshot = snapshot ?? new DeviceRegistrySnapshot();
    }

    public Task<DeviceRegistrySnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_snapshot);
    }

    public Task SaveAsync(DeviceRegistrySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _snapshot = snapshot;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingDisplaySwitcher : IDisplaySwitcher
{
    public int CallCount { get; private set; }

    public DisplayProfile? LastProfile { get; private set; }

    public Task<SwitchExecutionResult> ApplyAsync(DisplayProfile profile, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastProfile = profile;

        return Task.FromResult(SwitchExecutionResult.Succeeded(
            profile.DisplayProfileId,
            "Fake display switcher"));
    }
}

internal sealed class FailingDisplaySwitcher : IDisplaySwitcher
{
    public int CallCount { get; private set; }

    public Task<SwitchExecutionResult> ApplyAsync(DisplayProfile profile, CancellationToken cancellationToken = default)
    {
        CallCount++;

        return Task.FromResult(SwitchExecutionResult.Failure(
            profile.DisplayProfileId,
            "Fake display switcher",
            "Injected switch failure."));
    }
}

internal sealed class TestInputActivitySource : IInputActivitySource
{
    public event InputActivityObservedHandler? ActivityObserved;

    public Task PublishAsync(RuntimeDeviceObservation observation, CancellationToken cancellationToken = default)
    {
        return ActivityObserved?.Invoke(observation, cancellationToken) ?? Task.CompletedTask;
    }
}

internal sealed class RecordingOutcomeRecorder : ISwitchingOutcomeRecorder
{
    private readonly List<SwitchingOutcome> _outcomes = [];

    public IReadOnlyList<SwitchingOutcome> Outcomes => _outcomes;

    public Task RecordAsync(SwitchingOutcome outcome, CancellationToken cancellationToken = default)
    {
        _outcomes.Add(outcome);
        return Task.CompletedTask;
    }
}
