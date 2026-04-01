namespace InputAwareDisplaySwitcher.Core.Application;

public sealed class InMemorySwitchingOutcomeRecorder : ISwitchingOutcomeRecorder
{
    private readonly List<SwitchingOutcome> _outcomes = [];

    public IReadOnlyList<SwitchingOutcome> Outcomes => _outcomes;

    public Task RecordAsync(SwitchingOutcome outcome, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        _outcomes.Add(outcome);
        return Task.CompletedTask;
    }
}
