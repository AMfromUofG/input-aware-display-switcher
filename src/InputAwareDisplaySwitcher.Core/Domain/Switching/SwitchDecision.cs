namespace InputAwareDisplaySwitcher.Core.Domain.Switching;

public sealed record SwitchDecision
{
    public required SwitchDecisionStatus Status { get; init; }

    public required SwitchDecisionReason Reason { get; init; }

    public required string Message { get; init; }

    public required DateTimeOffset EvaluatedAtUtc { get; init; }

    public string? MatchedDeviceId { get; init; }

    public string? TargetZoneId { get; init; }

    public string? TargetDisplayProfileId { get; init; }

    public DateTimeOffset? CooldownEndsAtUtc { get; init; }

    public bool ShouldSwitch => Status == SwitchDecisionStatus.Allowed;
}
