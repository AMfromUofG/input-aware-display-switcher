using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Core.Application;

public sealed class InMemoryRuntimeStateStore : IRuntimeStateStore
{
    private ApplicationRuntimeState _state;

    public InMemoryRuntimeStateStore(ApplicationRuntimeState? initialState = null)
    {
        _state = initialState ?? new ApplicationRuntimeState();
    }

    public Task<ApplicationRuntimeState> LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_state);
    }

    public Task SaveAsync(ApplicationRuntimeState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
        return Task.CompletedTask;
    }
}
