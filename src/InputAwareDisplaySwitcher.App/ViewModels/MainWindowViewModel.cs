using System.Collections.ObjectModel;
using InputAwareDisplaySwitcher.Core.Application;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private NavigationItemViewModel? _selectedNavigation;
    private SectionViewModelBase _currentSection;
    private readonly IReadOnlyList<SectionViewModelBase> _sections;

    public MainWindowViewModel(
        AppConfigurationSession configurationSession,
        IInputDeviceSnapshotProvider snapshotProvider,
        DeviceManagementService deviceManagementService,
        IDiagnosticsService diagnostics,
        string configurationPath)
    {
        ArgumentNullException.ThrowIfNull(configurationSession);
        ArgumentNullException.ThrowIfNull(snapshotProvider);
        ArgumentNullException.ThrowIfNull(deviceManagementService);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationPath);

        WindowTitle = "Input Aware Display Switcher";
        ConfigurationPath = configurationPath;

        var configuration = configurationSession.CurrentConfiguration;
        var zonesProfiles = new ZonesProfilesViewModel(configurationSession);
        NavigationItemViewModel? zonesNavigationItem = null;
        var rules = new RulesSettingsViewModel(
            configurationSession,
            configuration.Preferences,
            configuration.SwitchingPolicy);
        var devices = new DevicesViewModel(
            configurationSession,
            snapshotProvider,
            deviceManagementService,
            openZonesProfiles: () => SelectedNavigation = zonesNavigationItem);
        var diagnosticsSection = new DiagnosticsViewModel(diagnostics);
        _sections = [devices, zonesProfiles, rules, diagnosticsSection];

        var devicesNavigationItem = new NavigationItemViewModel("Devices", "Live input devices, aliases, and zone assignments.", devices);
        zonesNavigationItem = new NavigationItemViewModel("Zones / Profiles", "Logical zones and display profile mappings.", zonesProfiles);
        var rulesNavigationItem = new NavigationItemViewModel("Rules / Settings", "Automation, cooldowns, and priority behaviour.", rules);
        var diagnosticsNavigationItem = new NavigationItemViewModel("Diagnostics", "Structured runtime and configuration history.", diagnosticsSection);

        NavigationItems =
        [
            devicesNavigationItem,
            zonesNavigationItem,
            rulesNavigationItem,
            diagnosticsNavigationItem
        ];

        _selectedNavigation = devicesNavigationItem;
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

    public void Dispose()
    {
        foreach (var section in _sections)
        {
            section.Dispose();
        }
    }
}
