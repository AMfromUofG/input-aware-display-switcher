using InputAwareDisplaySwitcher.Core.Domain.Configuration;

namespace InputAwareDisplaySwitcher.Core.Application;

public interface IAppConfigurationStore
{
    Task<AppConfiguration> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppConfiguration configuration, CancellationToken cancellationToken = default);
}
