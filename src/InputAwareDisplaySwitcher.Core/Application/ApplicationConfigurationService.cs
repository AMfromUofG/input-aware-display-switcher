using InputAwareDisplaySwitcher.Core.Domain.Configuration;

namespace InputAwareDisplaySwitcher.Core.Application;

public sealed class ApplicationConfigurationService
{
    private readonly IAppConfigurationStore _store;

    public ApplicationConfigurationService(IAppConfigurationStore store)
    {
        _store = store;
    }

    public Task<AppConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        return _store.LoadAsync(cancellationToken);
    }

    public Task SaveAsync(AppConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return _store.SaveAsync(configuration, cancellationToken);
    }

    public async Task<AppConfiguration> UpdateAsync(
        Func<AppConfiguration, AppConfiguration> update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        var current = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        var updated = update(current);
        await _store.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return updated;
    }
}
