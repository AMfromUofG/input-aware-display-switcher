using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Core.Application;

public sealed class AutomaticSwitchingController
{
    private readonly IInputActivitySource _inputActivitySource;
    private readonly SwitchingOrchestrator _orchestrator;
    private readonly IRuntimeStateStore _runtimeStateStore;
    private readonly ISwitchingOutcomeRecorder _outcomeRecorder;
    private readonly SwitchingPolicy _policy;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _isStarted;

    public AutomaticSwitchingController(
        IInputActivitySource inputActivitySource,
        SwitchingOrchestrator orchestrator,
        IRuntimeStateStore runtimeStateStore,
        ISwitchingOutcomeRecorder outcomeRecorder,
        SwitchingPolicy policy)
    {
        _inputActivitySource = inputActivitySource;
        _orchestrator = orchestrator;
        _runtimeStateStore = runtimeStateStore;
        _outcomeRecorder = outcomeRecorder;
        _policy = policy;
    }

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        _inputActivitySource.ActivityObserved += OnActivityObservedAsync;
        _isStarted = true;
    }

    public void Stop()
    {
        if (!_isStarted)
        {
            return;
        }

        _inputActivitySource.ActivityObserved -= OnActivityObservedAsync;
        _isStarted = false;
    }

    public async Task<SwitchingOutcome> HandleObservationAsync(
        Core.Domain.Devices.RuntimeDeviceObservation observation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var runtimeState = await _runtimeStateStore.LoadAsync(cancellationToken).ConfigureAwait(false);

            var outcome = await _orchestrator
                .ProcessAsync(observation, runtimeState, _policy, cancellationToken)
                .ConfigureAwait(false);

            var updatedState = BuildUpdatedState(runtimeState, outcome);

            await _runtimeStateStore.SaveAsync(updatedState, cancellationToken).ConfigureAwait(false);
            await _outcomeRecorder.RecordAsync(outcome, cancellationToken).ConfigureAwait(false);

            return outcome;
        }
        finally
        {
            _gate.Release();
        }
    }

    private Task OnActivityObservedAsync(Core.Domain.Devices.RuntimeDeviceObservation observation, CancellationToken cancellationToken)
    {
        return HandleObservationAsync(observation, cancellationToken);
    }

    private static ApplicationRuntimeState BuildUpdatedState(ApplicationRuntimeState currentState, SwitchingOutcome outcome)
    {
        return currentState with
        {
            CurrentZoneId = outcome.ExecutionResult.Success
                ? outcome.Decision.TargetZoneId
                : currentState.CurrentZoneId,
            CurrentDisplayProfileId = outcome.ExecutionResult.Success
                ? outcome.Decision.TargetDisplayProfileId
                : currentState.CurrentDisplayProfileId,
            LastSwitchAtUtc = outcome.ExecutionResult.Success
                ? outcome.ExecutionResult.RecordedAtUtc
                : currentState.LastSwitchAtUtc,
            LastMatchedDeviceId = outcome.Decision.MatchedDeviceId ?? currentState.LastMatchedDeviceId
        };
    }
}
