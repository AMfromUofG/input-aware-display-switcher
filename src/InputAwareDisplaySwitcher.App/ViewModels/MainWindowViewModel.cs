using System.Collections.ObjectModel;
using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Configuration;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private NavigationItemViewModel? _selectedNavigation;
    private SectionViewModelBase _currentSection;

    public MainWindowViewModel(AppConfiguration configuration, IDiagnosticsService diagnostics, string configurationPath)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationPath);

        WindowTitle = "Input Aware Display Switcher";
        ConfigurationPath = configurationPath;

        var devices = new DevicesViewModel(configuration.DeviceRegistry.Devices);
        var zonesProfiles = new ZonesProfilesViewModel(
            configuration.DeviceRegistry.Zones,
            configuration.DeviceRegistry.DisplayProfiles);
        var rules = new RulesSettingsViewModel(configuration.SwitchingPolicy, configuration.Preferences);
        var diagnosticsSection = new DiagnosticsViewModel(diagnostics);

        NavigationItems = new ObservableCollection<NavigationItemViewModel>
        {
            new("Devices", "Persisted device identities and zone assignments.", devices),
            new("Zones / Profiles", "Logical zones and display profile mappings.", zonesProfiles),
            new("Rules / Settings", "Switching policy, cooldowns, and lock state.", rules),
            new("Diagnostics", "Structured runtime and configuration history.", diagnosticsSection)
        };

        _selectedNavigation = NavigationItems[0];
        _currentSection = _selectedNavigation.Section;
    }

    public string WindowTitle { get; }

    public string ConfigurationPath { get; }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    public NavigationItemViewModel? SelectedNavigation
    {
        get => _selectedNavigation;
        set
        {
            if (!SetProperty(ref _selectedNavigation, value) || value is null)
            {
                return;
            }

            CurrentSection = value.Section;
        }
    }

    public SectionViewModelBase CurrentSection
    {
        get => _currentSection;
        private set => SetProperty(ref _currentSection, value);
    }
}
