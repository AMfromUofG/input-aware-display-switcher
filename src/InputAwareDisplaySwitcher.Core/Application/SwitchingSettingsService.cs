using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Core.Application;

public sealed class SwitchingSettingsService
{
    private const int MaximumSeconds = 86400;

    public SwitchingSettingsInput CreateInput(SwitchingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return new SwitchingSettingsInput
        {
            AutomationEnabled = policy.AutomationEnabled,
            CooldownSecondsText = ((int)policy.Cooldown.TotalSeconds).ToString(),
            RecentActivityThresholdSecondsText = ((int)policy.RecentActivityThreshold.TotalSeconds).ToString(),
            PriorityMode = policy.PriorityMode,
            ManualLockStopsSwitching = policy.ManualLockStopsSwitching,
            AllowSameProfileRefresh = policy.AllowSameProfileRefresh
        };
    }

    public SwitchingSettingsValidationResult Validate(SwitchingSettingsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var cooldownResult = ParseSeconds(input.CooldownSecondsText, "Cooldown");
        var recentActivityResult = ParseSeconds(input.RecentActivityThresholdSecondsText, "Recent activity threshold");
        var priorityError = Enum.IsDefined(input.PriorityMode)
            ? null
            : "Priority behaviour must be one of the supported options.";

        if (cooldownResult.ErrorMessage is not null
            || recentActivityResult.ErrorMessage is not null
            || priorityError is not null)
        {
            return new SwitchingSettingsValidationResult
            {
                CooldownError = cooldownResult.ErrorMessage,
                RecentActivityThresholdError = recentActivityResult.ErrorMessage,
                PriorityModeError = priorityError
            };
        }

        return new SwitchingSettingsValidationResult
        {
            Policy = new SwitchingPolicy
            {
                AutomationEnabled = input.AutomationEnabled,
                Cooldown = TimeSpan.FromSeconds(cooldownResult.Seconds),
                RecentActivityThreshold = TimeSpan.FromSeconds(recentActivityResult.Seconds),
                PriorityMode = input.PriorityMode,
                ManualLockStopsSwitching = input.ManualLockStopsSwitching,
                AllowSameProfileRefresh = input.AllowSameProfileRefresh
            }
        };
    }

    private static NumericValidationResult ParseSeconds(string? text, string label)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return NumericValidationResult.Invalid($"{label} is required.");
        }

        if (!int.TryParse(text, out var seconds))
        {
            return NumericValidationResult.Invalid($"{label} must be a whole number of seconds.");
        }

        if (seconds < 0)
        {
            return NumericValidationResult.Invalid($"{label} cannot be negative.");
        }

        if (seconds > MaximumSeconds)
        {
            return NumericValidationResult.Invalid($"{label} must stay at or below {MaximumSeconds} seconds.");
        }

        return NumericValidationResult.Valid(seconds);
    }

    private readonly record struct NumericValidationResult(int Seconds, string? ErrorMessage)
    {
        public static NumericValidationResult Valid(int seconds) => new(seconds, null);

        public static NumericValidationResult Invalid(string errorMessage) => new(0, errorMessage);
    }
}

public sealed record SwitchingSettingsInput
{
    public bool AutomationEnabled { get; init; } = true;

    public string CooldownSecondsText { get; init; } = "30";

    public string RecentActivityThresholdSecondsText { get; init; } = "15";

    public PriorityMode PriorityMode { get; init; } = PriorityMode.MostRecentInputWins;

    public bool ManualLockStopsSwitching { get; init; } = true;

    public bool AllowSameProfileRefresh { get; init; }
}

public sealed record SwitchingSettingsValidationResult
{
    public SwitchingPolicy? Policy { get; init; }

    public string? CooldownError { get; init; }

    public string? RecentActivityThresholdError { get; init; }

    public string? PriorityModeError { get; init; }

    public bool IsValid => Policy is not null;
}
