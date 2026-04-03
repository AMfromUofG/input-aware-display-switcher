using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Configuration;
using InputAwareDisplaySwitcher.Core.Domain.Devices;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class DevicesViewModel : SectionViewModelBase
{
    private readonly AppConfigurationSession _configurationSession;
    private readonly IInputDeviceSnapshotProvider _snapshotProvider;
    private readonly DeviceManagementService _deviceManagementService;
    private readonly Action? _openZonesProfiles;
    private readonly RelayCommand _refreshCommand;
    private readonly RelayCommand _openZonesProfilesCommand;
    private readonly DispatcherTimer _refreshTimer;
    private IReadOnlyList<RuntimeDeviceObservation> _runtimeDevices = [];
    private bool _isRefreshing;
    private string? _refreshStatusMessage;
    private bool _refreshStatusIsError;
    private DateTimeOffset? _lastRefreshedAtUtc;

    public DevicesViewModel(
        AppConfigurationSession configurationSession,
        IInputDeviceSnapshotProvider snapshotProvider,
        DeviceManagementService deviceManagementService,
        Action? openZonesProfiles = null)
        : base("Devices", "Detected input devices, app-local aliases, and zone assignments.")
    {
        _configurationSession = configurationSession ?? throw new ArgumentNullException(nameof(configurationSession));
        _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
        _deviceManagementService = deviceManagementService ?? throw new ArgumentNullException(nameof(deviceManagementService));
        _openZonesProfiles = openZonesProfiles;
        _refreshCommand = new RelayCommand(() => _ = RefreshAsync(), () => !IsRefreshing);
        _openZonesProfilesCommand = new RelayCommand(() => _openZonesProfiles?.Invoke(), () => _openZonesProfiles is not null);

        DeviceRows = [];
        ZoneOptions = [];

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(15)
        };
        _refreshTimer.Tick += (_, _) => _ = RefreshAsync();
        _refreshTimer.Start();

        _configurationSession.ConfigurationChanged += OnConfigurationChanged;
        RebuildRows(_configurationSession.CurrentConfiguration);
        _ = RefreshAsync();
    }

    public ObservableCollection<DeviceRowViewModel> DeviceRows { get; }

    public ObservableCollection<ZoneOptionViewModel> ZoneOptions { get; }

    public RelayCommand RefreshCommand => _refreshCommand;

    public RelayCommand OpenZonesProfilesCommand => _openZonesProfilesCommand;

    public bool HasDevices => DeviceRows.Count > 0;

    public bool HasZones => ZoneOptions.Count > 1;

    public int DeviceCount => DeviceRows.Count;

    public int DetectedThisSessionCount => DeviceRows.Count(device => device.IsDetectedThisSession);

    public int UnassignedDeviceCount => DeviceRows.Count(device => !device.IsAssigned);

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            if (!SetProperty(ref _isRefreshing, value))
            {
                return;
            }

            _refreshCommand.NotifyCanExecuteChanged();
        }
    }

    public string DeviceSummary => HasDevices
        ? $"{DeviceCount} device record(s), {DetectedThisSessionCount} detected now, {UnassignedDeviceCount} unassigned."
        : "No devices detected or remembered yet.";

    public string ZoneAssignmentHelpText => HasZones
        ? "Rename devices with app-local aliases and assign them to the zone that should control display switching."
        : "No zones are configured yet. Add a zone in Zones / Profiles before assigning devices.";

    public string RefreshStatusMessage
    {
        get => _refreshStatusMessage ?? "Waiting for the first device snapshot.";
        private set => SetProperty(ref _refreshStatusMessage, value);
    }

    public bool RefreshStatusIsError
    {
        get => _refreshStatusIsError;
        private set => SetProperty(ref _refreshStatusIsError, value);
    }

    public string LastRefreshedText => _lastRefreshedAtUtc.HasValue
        ? $"Last refreshed {_lastRefreshedAtUtc.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}"
        : "No live snapshot yet.";

    private async Task RefreshAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;

        try
        {
            _runtimeDevices = await _snapshotProvider.GetCurrentDevicesAsync().ConfigureAwait(true);
            _lastRefreshedAtUtc = DateTimeOffset.UtcNow;
            OnPropertyChanged(nameof(LastRefreshedText));

            RefreshStatusMessage = _runtimeDevices.Count == 0
                ? "No active keyboards or mice were found in the latest snapshot. Saved mappings stay visible so you can keep managing them."
                : "Live device snapshot updated.";
            RefreshStatusIsError = false;
            RebuildRows(_configurationSession.CurrentConfiguration);
        }
        catch (Exception exception)
        {
            RefreshStatusMessage = $"Device snapshot failed: {exception.Message}";
            RefreshStatusIsError = true;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task PersistEditAsync(DeviceEditRequest request)
    {
        try
        {
            await _configurationSession.UpdateAsync(current => current with
            {
                DeviceRegistry = _deviceManagementService.ApplyEdits(
                    current.DeviceRegistry,
                    request.Entry,
                    request.FriendlyName,
                    request.AssignedZoneId,
                    DateTimeOffset.UtcNow)
            }).ConfigureAwait(true);

            RefreshStatusMessage = $"Saved changes for '{request.FriendlyName}'.";
            RefreshStatusIsError = false;
        }
        catch (Exception exception)
        {
            RefreshStatusMessage = $"Could not save device changes: {exception.Message}";
            RefreshStatusIsError = true;
            RebuildRows(_configurationSession.CurrentConfiguration);
        }
    }

    private void OnConfigurationChanged(AppConfiguration configuration)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            RebuildRows(configuration);
            return;
        }

        dispatcher.Invoke(() => RebuildRows(configuration));
    }

    private void RebuildRows(AppConfiguration configuration)
    {
        DeviceRows.Clear();
        ZoneOptions.Clear();

        ZoneOptions.Add(ZoneOptionViewModel.CreateUnassigned());
        foreach (var zone in configuration.DeviceRegistry.Zones.OrderBy(zone => zone.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            ZoneOptions.Add(new ZoneOptionViewModel(zone));
        }

        var entries = _deviceManagementService.BuildEntries(configuration.DeviceRegistry, _runtimeDevices);
        foreach (var entry in entries)
        {
            DeviceRows.Add(new DeviceRowViewModel(entry, request => _ = PersistEditAsync(request)));
        }

        OnPropertyChanged(nameof(HasDevices));
        OnPropertyChanged(nameof(HasZones));
        OnPropertyChanged(nameof(DeviceCount));
        OnPropertyChanged(nameof(DetectedThisSessionCount));
        OnPropertyChanged(nameof(UnassignedDeviceCount));
        OnPropertyChanged(nameof(DeviceSummary));
        OnPropertyChanged(nameof(ZoneAssignmentHelpText));
    }

    public override void Dispose()
    {
        _refreshTimer.Stop();
        _configurationSession.ConfigurationChanged -= OnConfigurationChanged;
    }
}
