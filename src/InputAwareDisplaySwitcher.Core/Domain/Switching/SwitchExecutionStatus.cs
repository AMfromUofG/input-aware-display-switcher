namespace InputAwareDisplaySwitcher.Core.Domain.Switching;

public enum SwitchExecutionStatus
{
    NotAttempted = 0,
    Succeeded = 1,
    Failed = 2,
    Unsupported = 3
}
