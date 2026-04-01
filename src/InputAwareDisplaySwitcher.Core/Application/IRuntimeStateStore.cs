using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Core.Application;

public interface IRuntimeStateStore
{
    Task<ApplicationRuntimeState> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ApplicationRuntimeState state, CancellationToken cancellationToken = default);
}
