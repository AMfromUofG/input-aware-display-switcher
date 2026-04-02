using InputAwareDisplaySwitcher.Core.Abstractions;
using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;
using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Core.Application;

public sealed class SwitchingOrchestrator
{
    private readonly DeviceRegistryService _deviceRegistryService;
    private readonly IDecisionEngine _decisionEngine;
    private readonly IDisplaySwitcher _displaySwitcher;
    private readonly IDiagnosticsService _diagnostics;

    public SwitchingOrchestrator(
        DeviceRegistryService deviceRegistryService,
        IDecisionEngine decisionEngine,
        IDisplaySwitcher displaySwitcher,
        IDiagnosticsService? diagnostics = null)
    {
        _deviceRegistryService = deviceRegistryService;
        _decisionEngine = decisionEngine;
        _displaySwitcher = displaySwitcher;
        _diagnostics = diagnostics ?? NullDiagnosticsService.Instance;
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

        _diagnostics.Record(
            DiagnosticCategories.Switching,
            DiagnosticEventTypes.DeviceResolutionCompleted,
            resolution.Message ?? "Device resolution completed.",
            severity: resolution.Status == DeviceRegistryResolutionStatus.Resolved
                ? DiagnosticSeverity.Information
                : DiagnosticSeverity.Warning,
            details: CreateResolutionDetails(resolution));

        var decision = _decisionEngine.Evaluate(new DecisionRequest
        {
            Observation = observation,
            Resolution = resolution,
            RuntimeState = runtimeState,
            Policy = policy,
            EvaluatedAtUtc = observation.ObservedAtUtc
        });

        _diagnostics.Record(
            DiagnosticCategories.Switching,
            DiagnosticEventTypes.SwitchDecisionEvaluated,
            "Switching decision evaluated.",
            severity: decision.Status == SwitchDecisionStatus.Blocked
                ? DiagnosticSeverity.Warning
                : DiagnosticSeverity.Information,
            details: CreateDecisionDetails(decision, runtimeState));

        var executionResult = SwitchExecutionResult.NotAttempted(
            decision.TargetDisplayProfileId,
            "No display switch was attempted.");

        if (decision.ShouldSwitch && resolution.TargetProfile is not null)
        {
            _diagnostics.Record(
                DiagnosticCategories.Switching,
                DiagnosticEventTypes.SwitchAttempted,
                "Display switch attempt started.",
                details: CreateSwitchAttemptDetails(decision, resolution));

            executionResult = await _displaySwitcher
                .ApplyAsync(resolution.TargetProfile, cancellationToken)
                .ConfigureAwait(false);

            RecordExecutionResult(executionResult, decision);
        }
        else if (decision.Status == SwitchDecisionStatus.Blocked)
        {
            _diagnostics.Record(
                DiagnosticCategories.Switching,
                DiagnosticEventTypes.SwitchBlocked,
                decision.Message,
                DiagnosticSeverity.Warning,
                CreateBlockedDetails(decision));
        }
        else
        {
            _diagnostics.Record(
                DiagnosticCategories.Switching,
                DiagnosticEventTypes.SwitchNoAction,
                decision.Message,
                details: CreateBlockedDetails(decision));
        }

        return new SwitchingOutcome
        {
            Observation = observation,
            Resolution = resolution,
            Decision = decision,
            ExecutionResult = executionResult
        };
    }

    private void RecordExecutionResult(SwitchExecutionResult executionResult, SwitchDecision decision)
    {
        switch (executionResult.Status)
        {
            case SwitchExecutionStatus.Succeeded:
                _diagnostics.Record(
                    DiagnosticCategories.Switching,
                    DiagnosticEventTypes.SwitchSucceeded,
                    "Display switch completed successfully.",
                    details: CreateExecutionDetails(executionResult, decision));
                break;
            case SwitchExecutionStatus.Failed:
                _diagnostics.Record(
                    DiagnosticCategories.Switching,
                    DiagnosticEventTypes.SwitchFailed,
                    executionResult.ErrorMessage ?? "Display switch failed.",
                    DiagnosticSeverity.Error,
                    CreateExecutionDetails(executionResult, decision));
                break;
            default:
                _diagnostics.Record(
                    DiagnosticCategories.Switching,
                    DiagnosticEventTypes.SwitchNotApplied,
                    executionResult.ErrorMessage ?? "Display switch was not applied.",
                    DiagnosticSeverity.Warning,
                    CreateExecutionDetails(executionResult, decision));
                break;
        }
    }

    private static IReadOnlyDictionary<string, string?> CreateResolutionDetails(DeviceRegistryResolution resolution)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["resolutionStatus"] = resolution.Status.ToString(),
            ["matchedDeviceId"] = resolution.MatchedDevice?.DeviceId,
            ["zoneId"] = resolution.Zone?.ZoneId,
            ["profileId"] = resolution.TargetProfile?.DisplayProfileId,
            ["matchedByKey"] = resolution.MatchedByPersistenceKey
        };
    }

    private static IReadOnlyDictionary<string, string?> CreateDecisionDetails(
        SwitchDecision decision,
        ApplicationRuntimeState runtimeState)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["decisionStatus"] = decision.Status.ToString(),
            ["reason"] = decision.Reason.ToString(),
            ["matchedDeviceId"] = decision.MatchedDeviceId,
            ["targetZoneId"] = decision.TargetZoneId,
            ["targetDisplayProfileId"] = decision.TargetDisplayProfileId,
            ["cooldownEndsAtUtc"] = decision.CooldownEndsAtUtc?.ToString("O"),
            ["currentZoneId"] = runtimeState.CurrentZoneId,
            ["currentDisplayProfileId"] = runtimeState.CurrentDisplayProfileId,
            ["isManualSwitchingLocked"] = runtimeState.IsManualSwitchingLocked.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string?> CreateSwitchAttemptDetails(
        SwitchDecision decision,
        DeviceRegistryResolution resolution)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["matchedDeviceId"] = decision.MatchedDeviceId,
            ["targetZoneId"] = decision.TargetZoneId,
            ["targetDisplayProfileId"] = decision.TargetDisplayProfileId,
            ["profileName"] = resolution.TargetProfile?.Name,
            ["profileIntentKind"] = resolution.TargetProfile?.IntentKind.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string?> CreateBlockedDetails(SwitchDecision decision)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["decisionStatus"] = decision.Status.ToString(),
            ["reason"] = decision.Reason.ToString(),
            ["matchedDeviceId"] = decision.MatchedDeviceId,
            ["targetZoneId"] = decision.TargetZoneId,
            ["targetDisplayProfileId"] = decision.TargetDisplayProfileId,
            ["cooldownEndsAtUtc"] = decision.CooldownEndsAtUtc?.ToString("O")
        };
    }

    private static IReadOnlyDictionary<string, string?> CreateExecutionDetails(
        SwitchExecutionResult executionResult,
        SwitchDecision decision)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["decisionReason"] = decision.Reason.ToString(),
            ["targetZoneId"] = decision.TargetZoneId,
            ["targetDisplayProfileId"] = executionResult.DisplayProfileId ?? decision.TargetDisplayProfileId,
            ["executionStatus"] = executionResult.Status.ToString(),
            ["executionPath"] = executionResult.ExecutionPath,
            ["errorCode"] = executionResult.ErrorCode?.ToString(),
            ["errorMessage"] = executionResult.ErrorMessage,
            ["recordedAtUtc"] = executionResult.RecordedAtUtc.ToString("O")
        };
    }
}
