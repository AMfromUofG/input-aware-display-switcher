using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Core.Abstractions;

public interface IDisplaySwitcher
{
    Task<SwitchExecutionResult> ApplyAsync(DisplayProfile profile, CancellationToken cancellationToken = default);
}
