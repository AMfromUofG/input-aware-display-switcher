using InputAwareDisplaySwitcher.Core.Domain.Configuration;

namespace InputAwareDisplaySwitcher.Core.Application;

public delegate void AppConfigurationChangedHandler(AppConfiguration configuration);

public sealed class AppConfigurationSession
{
    private readonly ApplicationConfigurationService _configurationService;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AppConfiguration _currentConfiguration;

    public AppConfigurationSession(
        ApplicationConfigurationService configurationService,
        AppConfiguration initialConfiguration)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _currentConfiguration = initialConfiguration ?? throw new ArgumentNullException(nameof(initialConfiguration));
    }

    public event AppConfigurationChangedHandler? ConfigurationChanged;

    public AppConfiguration CurrentConfiguration => _currentConfiguration;

    public async Task<AppConfiguration> UpdateAsync(
        Func<AppConfiguration, AppConfiguration> update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var updatedConfiguration = update(_currentConfiguration);
            await _configurationService.SaveAsync(updatedConfiguration, cancellationToken).ConfigureAwait(false);
            _currentConfiguration = updatedConfiguration;
        }
        finally
        {
            _gate.Release();
        }

        ConfigurationChanged?.Invoke(_currentConfiguration);
        return _currentConfiguration;
    }

    public async Task<AppConfiguration> ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _currentConfiguration = await _configurationService.LoadAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }

        ConfigurationChanged?.Invoke(_currentConfiguration);
        return _currentConfiguration;
    }
}
