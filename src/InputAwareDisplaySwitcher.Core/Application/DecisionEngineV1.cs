using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Core.Application;

public sealed class DecisionEngineV1 : IDecisionEngine
{
    public SwitchDecision Evaluate(DecisionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resolution = request.Resolution;
        var now = request.EvaluatedAtUtc;

        switch (resolution.Status)
        {
            case DeviceRegistryResolutionStatus.UnknownDevice:
                return Blocked(request, SwitchDecisionReason.UnknownDevice, resolution.Message ?? "The device is unknown.");
            case DeviceRegistryResolutionStatus.DeviceDisabled:
                return Blocked(request, SwitchDecisionReason.DisabledDevice, resolution.Message ?? "The matched device is disabled.");
            case DeviceRegistryResolutionStatus.UnmappedZone:
                return Blocked(request, SwitchDecisionReason.UnmappedDevice, resolution.Message ?? "The matched device is not mapped to a zone.");
            case DeviceRegistryResolutionStatus.ZoneDisabled:
                return Blocked(request, SwitchDecisionReason.DisabledZone, resolution.Message ?? "The resolved zone is disabled.");
            case DeviceRegistryResolutionStatus.ProfileUnavailable:
                return Blocked(request, SwitchDecisionReason.MissingDisplayProfile, resolution.Message ?? "The resolved display profile is unavailable.");
        }

        if (request.Policy.ManualLockStopsSwitching && request.RuntimeState.IsManualSwitchingLocked)
        {
            return Blocked(request, SwitchDecisionReason.ManualLockActive, "Automatic switching is currently locked.");
        }

        if (request.Policy.Cooldown > TimeSpan.Zero && request.RuntimeState.LastSwitchAtUtc.HasValue)
        {
            var cooldownEndsAtUtc = request.RuntimeState.LastSwitchAtUtc.Value.Add(request.Policy.Cooldown);
            if (cooldownEndsAtUtc > now)
            {
                return new SwitchDecision
                {
                    Status = SwitchDecisionStatus.Blocked,
                    Reason = SwitchDecisionReason.CooldownActive,
                    Message = $"Cooldown is active until {cooldownEndsAtUtc:O}.",
                    EvaluatedAtUtc = now,
                    MatchedDeviceId = resolution.MatchedDevice?.DeviceId,
                    TargetZoneId = resolution.Zone?.ZoneId,
                    TargetDisplayProfileId = resolution.TargetProfile?.DisplayProfileId,
                    CooldownEndsAtUtc = cooldownEndsAtUtc
                };
            }
        }

        var sameZone = !string.IsNullOrWhiteSpace(request.RuntimeState.CurrentZoneId)
            && string.Equals(request.RuntimeState.CurrentZoneId, resolution.Zone?.ZoneId, StringComparison.OrdinalIgnoreCase);
        var sameProfile = !string.IsNullOrWhiteSpace(request.RuntimeState.CurrentDisplayProfileId)
            && string.Equals(request.RuntimeState.CurrentDisplayProfileId, resolution.TargetProfile?.DisplayProfileId, StringComparison.OrdinalIgnoreCase);

        var sameTargetAlreadyActive = sameProfile
            || (sameZone && string.IsNullOrWhiteSpace(request.RuntimeState.CurrentDisplayProfileId));

        if (!request.Policy.AllowSameProfileRefresh && sameTargetAlreadyActive)
        {
            return new SwitchDecision
            {
                Status = SwitchDecisionStatus.NoAction,
                Reason = SwitchDecisionReason.AlreadyActive,
                Message = "The resolved zone/profile is already active, so no switch is needed.",
                EvaluatedAtUtc = now,
                MatchedDeviceId = resolution.MatchedDevice?.DeviceId,
                TargetZoneId = resolution.Zone?.ZoneId,
                TargetDisplayProfileId = resolution.TargetProfile?.DisplayProfileId
            };
        }

        return new SwitchDecision
        {
            Status = SwitchDecisionStatus.Allowed,
            Reason = SwitchDecisionReason.Allowed,
            Message = "Switching is allowed for the resolved device, zone, and display profile.",
            EvaluatedAtUtc = now,
            MatchedDeviceId = resolution.MatchedDevice?.DeviceId,
            TargetZoneId = resolution.Zone?.ZoneId,
            TargetDisplayProfileId = resolution.TargetProfile?.DisplayProfileId
        };
    }

    private static SwitchDecision Blocked(DecisionRequest request, SwitchDecisionReason reason, string message)
    {
        return new SwitchDecision
        {
            Status = SwitchDecisionStatus.Blocked,
            Reason = reason,
            Message = message,
            EvaluatedAtUtc = request.EvaluatedAtUtc,
            MatchedDeviceId = request.Resolution.MatchedDevice?.DeviceId,
            TargetZoneId = request.Resolution.Zone?.ZoneId,
            TargetDisplayProfileId = request.Resolution.TargetProfile?.DisplayProfileId
        };
    }
}
