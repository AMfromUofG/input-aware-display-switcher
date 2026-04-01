namespace InputAwareDisplaySwitcher.Core.Domain.Profiles;

public enum DisplayProfileIntentKind
{
    Unknown = 0,
    DeskOnly = 1,
    LivingRoomOnly = 2,
    InternalOnly = 3,
    ExternalOnly = 4,
    Extend = 5,
    Clone = 6,
    SafeRestore = 7
}
