using InputAwareDisplaySwitcher.Core.Abstractions;
using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Core.Application;

public sealed class SwitchingOrchestrator
{
    private readonly DeviceRegistryService _deviceRegistryService;
    private readonly IDecisionEngine _decisionEngine;
    private readonly IDisplaySwitcher _displaySwitcher;

    public SwitchingOrchestrator(
        DeviceRegistryService deviceRegistryService,
        IDecisionEngine decisionEngine,
        IDisplaySwitcher displaySwitcher)
    {
        _deviceRegistryService = deviceRegistryService;
        _decisionEngine = decisionEngine;
        _displaySwitcher = displaySwitcher;
    }

    public async Task<SwitchingOutcome> ProcessAsync(
        RuntimeDeviceObservation observation,
        ApplicationRuntimeState runtimeState,
        SwitchingPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(runtimeState);
        ArgumentNullException.ThrowIfNull(policy);

        var snapshot = await _deviceRegistryService.LoadAsync(cancellationToken).ConfigureAwait(false);
        var resolution = _deviceRegistryService.Resolve(observation, snapshot);

        var decision = _decisionEngine.Evaluate(new DecisionRequest
        {
            Observation = observation,
            Resolution = resolution,
            RuntimeState = runtimeState,
            Policy = policy,
            EvaluatedAtUtc = observation.ObservedAtUtc
        });

        var executionResult = SwitchExecutionResult.NotAttempted(
            decision.TargetDisplayProfileId,
            "No display switch was attempted.");

        if (decision.ShouldSwitch && resolution.TargetProfile is not null)
        {
            executionResult = await _displaySwitcher
                .ApplyAsync(resolution.TargetProfile, cancellationToken)
                .ConfigureAwait(false);
        }

        return new SwitchingOutcome
        {
            Observation = observation,
            Resolution = resolution,
            Decision = decision,
            ExecutionResult = executionResult
        };
    }
}
