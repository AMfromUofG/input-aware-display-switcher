using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;
using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Core.Application;

public sealed class AutomaticSwitchingController
{
    private readonly IInputActivitySource _inputActivitySource;
    private readonly SwitchingOrchestrator _orchestrator;
    private readonly IRuntimeStateStore _runtimeStateStore;
    private readonly ISwitchingOutcomeRecorder _outcomeRecorder;
    private readonly SwitchingPolicy _policy;
    private readonly IDiagnosticsService _diagnostics;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _isStarted;

    public AutomaticSwitchingController(
        IInputActivitySource inputActivitySource,
        SwitchingOrchestrator orchestrator,
        IRuntimeStateStore runtimeStateStore,
        ISwitchingOutcomeRecorder outcomeRecorder,
        SwitchingPolicy policy,
        IDiagnosticsService? diagnostics = null)
    {
        _inputActivitySource = inputActivitySource;
        _orchestrator = orchestrator;
        _runtimeStateStore = runtimeStateStore;
        _outcomeRecorder = outcomeRecorder;
        _policy = policy;
        _diagnostics = diagnostics ?? NullDiagnosticsService.Instance;
    }

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        _inputActivitySource.ActivityObserved += OnActivityObservedAsync;
        _isStarted = true;
        _diagnostics.Record(
            DiagnosticCategories.Application,
            DiagnosticEventTypes.SwitchingControllerStarted,
            "Automatic switching controller started.",
            details: CreatePolicyDetails(_policy));
    }

    public void Stop()
    {
        if (!_isStarted)
        {
            return;
        }

        _inputActivitySource.ActivityObserved -= OnActivityObservedAsync;
        _isStarted = false;
        _diagnostics.Record(
            DiagnosticCategories.Application,
            DiagnosticEventTypes.SwitchingControllerStopped,
            "Automatic switching controller stopped.");
    }

    public async Task<SwitchingOutcome> HandleObservationAsync(
        RuntimeDeviceObservation observation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);

        _diagnostics.Record(
            DiagnosticCategories.Input,
            DiagnosticEventTypes.InputActivityDetected,
            "Input activity was observed.",
            details: CreateObservationDetails(observation));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var runtimeState = await _runtimeStateStore.LoadAsync(cancellationToken).ConfigureAwait(false);

            _diagnostics.Record(
                DiagnosticCategories.Switching,
                DiagnosticEventTypes.RuntimeStateLoaded,
                "Runtime state loaded for switching evaluation.",
                details: CreateRuntimeStateDetails(runtimeState));

            var outcome = await _orchestrator
                .ProcessAsync(observation, runtimeState, _policy, cancellationToken)
                .ConfigureAwait(false);

            var updatedState = BuildUpdatedState(runtimeState, outcome);

            await _runtimeStateStore.SaveAsync(updatedState, cancellationToken).ConfigureAwait(false);
            await _outcomeRecorder.RecordAsync(outcome, cancellationToken).ConfigureAwait(false);

            _diagnostics.Record(
                DiagnosticCategories.Switching,
                DiagnosticEventTypes.RuntimeStateUpdated,
                "Runtime state updated after processing input.",
                details: CreateStateTransitionDetails(runtimeState, updatedState, outcome));

            return outcome;
        }
        finally
        {
            _gate.Release();
        }
    }

    private Task OnActivityObservedAsync(RuntimeDeviceObservation observation, CancellationToken cancellationToken)
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
            CurrentZonePriority = outcome.ExecutionResult.Success
                ? outcome.Resolution.Zone?.Priority ?? currentState.CurrentZonePriority
                : currentState.CurrentZonePriority,
            CurrentDisplayProfileId = outcome.ExecutionResult.Success
                ? outcome.Decision.TargetDisplayProfileId
                : currentState.CurrentDisplayProfileId,
            LastSwitchAtUtc = outcome.ExecutionResult.Success
                ? outcome.ExecutionResult.RecordedAtUtc
                : currentState.LastSwitchAtUtc,
            LastInputAtUtc = outcome.Observation.ObservedAtUtc,
            LastInputZoneId = outcome.Resolution.Zone?.ZoneId ?? currentState.LastInputZoneId,
            LastMatchedDeviceId = outcome.Decision.MatchedDeviceId ?? currentState.LastMatchedDeviceId
        };
    }

    private static IReadOnlyDictionary<string, string?> CreateObservationDetails(RuntimeDeviceObservation observation)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["sessionDeviceId"] = observation.SessionDeviceId,
            ["friendlyName"] = observation.FriendlyName,
            ["deviceKind"] = observation.DeviceKind.ToString(),
            ["instanceId"] = observation.InstanceId,
            ["rawDevicePath"] = observation.RawDevicePath,
            ["normalizedDevicePath"] = observation.NormalizedDevicePath,
            ["vendorId"] = observation.VendorId,
            ["productId"] = observation.ProductId,
            ["observedAtUtc"] = observation.ObservedAtUtc.ToString("O")
        };
    }

    private static IReadOnlyDictionary<string, string?> CreatePolicyDetails(SwitchingPolicy policy)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["automationEnabled"] = policy.AutomationEnabled.ToString(),
            ["cooldown"] = policy.Cooldown.ToString(),
            ["recentActivityThreshold"] = policy.RecentActivityThreshold.ToString(),
            ["priorityMode"] = policy.PriorityMode.ToString(),
            ["manualLockStopsSwitching"] = policy.ManualLockStopsSwitching.ToString(),
            ["allowSameProfileRefresh"] = policy.AllowSameProfileRefresh.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string?> CreateRuntimeStateDetails(ApplicationRuntimeState state)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["currentZoneId"] = state.CurrentZoneId,
            ["currentZonePriority"] = state.CurrentZonePriority.ToString(),
            ["currentDisplayProfileId"] = state.CurrentDisplayProfileId,
            ["lastSwitchAtUtc"] = state.LastSwitchAtUtc?.ToString("O"),
            ["lastInputAtUtc"] = state.LastInputAtUtc?.ToString("O"),
            ["lastInputZoneId"] = state.LastInputZoneId,
            ["isManualSwitchingLocked"] = state.IsManualSwitchingLocked.ToString(),
            ["lastMatchedDeviceId"] = state.LastMatchedDeviceId
        };
    }

    private static IReadOnlyDictionary<string, string?> CreateStateTransitionDetails(
        ApplicationRuntimeState previousState,
        ApplicationRuntimeState updatedState,
        SwitchingOutcome outcome)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["previousZoneId"] = previousState.CurrentZoneId,
            ["updatedZoneId"] = updatedState.CurrentZoneId,
            ["previousZonePriority"] = previousState.CurrentZonePriority.ToString(),
            ["updatedZonePriority"] = updatedState.CurrentZonePriority.ToString(),
            ["previousDisplayProfileId"] = previousState.CurrentDisplayProfileId,
            ["updatedDisplayProfileId"] = updatedState.CurrentDisplayProfileId,
            ["previousLastSwitchAtUtc"] = previousState.LastSwitchAtUtc?.ToString("O"),
            ["updatedLastSwitchAtUtc"] = updatedState.LastSwitchAtUtc?.ToString("O"),
            ["previousLastInputAtUtc"] = previousState.LastInputAtUtc?.ToString("O"),
            ["updatedLastInputAtUtc"] = updatedState.LastInputAtUtc?.ToString("O"),
            ["previousLastInputZoneId"] = previousState.LastInputZoneId,
            ["updatedLastInputZoneId"] = updatedState.LastInputZoneId,
            ["previousLastMatchedDeviceId"] = previousState.LastMatchedDeviceId,
            ["updatedLastMatchedDeviceId"] = updatedState.LastMatchedDeviceId,
            ["decisionStatus"] = outcome.Decision.Status.ToString(),
            ["decisionReason"] = outcome.Decision.Reason.ToString(),
            ["executionStatus"] = outcome.ExecutionResult.Status.ToString()
        };
    }
}
