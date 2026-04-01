namespace InputAwareDisplaySwitcher.Core.Domain.Switching;

public enum SwitchDecisionReason
{
    Allowed = 0,
    UnknownDevice = 1,
    DisabledDevice = 2,
    UnmappedDevice = 3,
    DisabledZone = 4,
    MissingDisplayProfile = 5,
    ManualLockActive = 6,
    CooldownActive = 7,
    AlreadyActive = 8
}
