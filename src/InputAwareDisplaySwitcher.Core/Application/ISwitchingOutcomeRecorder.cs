namespace InputAwareDisplaySwitcher.Core.Application;

public interface ISwitchingOutcomeRecorder
{
    Task RecordAsync(SwitchingOutcome outcome, CancellationToken cancellationToken = default);
}
