namespace InputAwareDisplaySwitcher.Core.Domain.Switching;

public enum SwitchDecisionReason
{
    Allowed = 0,
    AutomationDisabled = 1,
    UnknownDevice = 2,
    DisabledDevice = 3,
    UnmappedDevice = 4,
    DisabledZone = 5,
    MissingDisplayProfile = 6,
    ManualLockActive = 7,
    CooldownActive = 8,
    PrioritySuppressed = 9,
    AlreadyActive = 10
}
