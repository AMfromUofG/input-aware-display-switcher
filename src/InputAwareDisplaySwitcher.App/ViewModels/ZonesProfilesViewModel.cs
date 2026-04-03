using System.Collections.ObjectModel;
using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Configuration;
using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Zones;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class ZonesProfilesViewModel : SectionViewModelBase
{
    private readonly AppConfigurationSession _configurationSession;
    private readonly RelayCommand _addProfileCommand;
    private readonly RelayCommand _addZoneCommand;
    private string _newProfileName = string.Empty;
    private string _newProfileDescription = string.Empty;
    private DisplayProfileIntentKind _newProfileIntentKind = DisplayProfileIntentKind.ExternalOnly;
    private string _newZoneName = string.Empty;
    private string _newZoneDescription = string.Empty;
    private string _newZonePriorityText = "0";
    private ZoneOptionViewModel? _selectedProfileForZone;
    private string? _statusMessage;
    private bool _statusIsError;

    public ZonesProfilesViewModel(AppConfigurationSession configurationSession)
        : base("Zones / Profiles", "Logical zones and the display intents they resolve to.")
    {
        _configurationSession = configurationSession ?? throw new ArgumentNullException(nameof(configurationSession));
        _addProfileCommand = new RelayCommand(() => _ = AddProfileAsync());
        _addZoneCommand = new RelayCommand(() => _ = AddZoneAsync());

        Zones = [];
        Profiles = [];
        ProfileOptions = [];
        ProfileIntentKinds = Enum.GetValues<DisplayProfileIntentKind>();

        _configurationSession.ConfigurationChanged += OnConfigurationChanged;
        ApplyConfiguration(_configurationSession.CurrentConfiguration);
    }

    public ObservableCollection<ZoneDefinition> Zones { get; }

    public ObservableCollection<DisplayProfile> Profiles { get; }

    public ObservableCollection<ZoneOptionViewModel> ProfileOptions { get; }

    public IReadOnlyList<DisplayProfileIntentKind> ProfileIntentKinds { get; }

    public RelayCommand AddProfileCommand => _addProfileCommand;

    public RelayCommand AddZoneCommand => _addZoneCommand;

    public string NewProfileName
    {
        get => _newProfileName;
        set => SetProperty(ref _newProfileName, value);
    }

    public string NewProfileDescription
    {
        get => _newProfileDescription;
        set => SetProperty(ref _newProfileDescription, value);
    }

    public DisplayProfileIntentKind NewProfileIntentKind
    {
        get => _newProfileIntentKind;
        set => SetProperty(ref _newProfileIntentKind, value);
    }

    public string NewZoneName
    {
        get => _newZoneName;
        set => SetProperty(ref _newZoneName, value);
    }

    public string NewZoneDescription
    {
        get => _newZoneDescription;
        set => SetProperty(ref _newZoneDescription, value);
    }

    public string NewZonePriorityText
    {
        get => _newZonePriorityText;
        set => SetProperty(ref _newZonePriorityText, value);
    }

    public ZoneOptionViewModel? SelectedProfileForZone
    {
        get => _selectedProfileForZone;
        set => SetProperty(ref _selectedProfileForZone, value);
    }

    public string StatusMessage
    {
        get => _statusMessage ?? "Add at least one profile and zone so device assignments have somewhere to point.";
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool StatusIsError
    {
        get => _statusIsError;
        private set => SetProperty(ref _statusIsError, value);
    }

    public bool HasProfiles => Profiles.Count > 0;

    private async Task AddProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName))
        {
            StatusMessage = "Profile name is required.";
            StatusIsError = true;
            return;
        }

        var profile = new DisplayProfile
        {
            DisplayProfileId = CreateSlug(NewProfileName, Profiles.Select(existing => existing.DisplayProfileId)),
            Name = NewProfileName.Trim(),
            Description = string.IsNullOrWhiteSpace(NewProfileDescription) ? null : NewProfileDescription.Trim(),
            IntentKind = NewProfileIntentKind
        };

        await _configurationSession.UpdateAsync(current => current with
        {
            DeviceRegistry = current.DeviceRegistry with
            {
                DisplayProfiles = current.DeviceRegistry.DisplayProfiles
                    .Append(profile)
                    .ToList()
            }
        }).ConfigureAwait(true);

        NewProfileName = string.Empty;
        NewProfileDescription = string.Empty;
        NewProfileIntentKind = DisplayProfileIntentKind.ExternalOnly;
        StatusMessage = $"Added profile '{profile.Name}'.";
        StatusIsError = false;
    }

    private async Task AddZoneAsync()
    {
        if (!HasProfiles)
        {
            StatusMessage = "Add a display profile first, then create a zone that points to it.";
            StatusIsError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(NewZoneName))
        {
            StatusMessage = "Zone name is required.";
            StatusIsError = true;
            return;
        }

        if (SelectedProfileForZone?.ZoneId is null)
        {
            StatusMessage = "Choose the display profile this zone should resolve to.";
            StatusIsError = true;
            return;
        }

        if (!int.TryParse(NewZonePriorityText, out var priority) || priority < 0)
        {
            StatusMessage = "Zone priority must be a non-negative whole number.";
            StatusIsError = true;
            return;
        }

        var zone = new ZoneDefinition
        {
            ZoneId = CreateSlug(NewZoneName, Zones.Select(existing => existing.ZoneId)),
            Name = NewZoneName.Trim(),
            PreferredDisplayProfileId = SelectedProfileForZone.ZoneId,
            Priority = priority,
            Description = string.IsNullOrWhiteSpace(NewZoneDescription) ? null : NewZoneDescription.Trim()
        };

        await _configurationSession.UpdateAsync(current => current with
        {
            DeviceRegistry = current.DeviceRegistry with
            {
                Zones = current.DeviceRegistry.Zones
                    .Append(zone)
                    .ToList()
            }
        }).ConfigureAwait(true);

        NewZoneName = string.Empty;
        NewZoneDescription = string.Empty;
        NewZonePriorityText = "0";
        SelectedProfileForZone = ProfileOptions.FirstOrDefault();
        StatusMessage = $"Added zone '{zone.Name}'.";
        StatusIsError = false;
    }

    private void ApplyConfiguration(AppConfiguration configuration)
    {
        Zones.Clear();
        foreach (var zone in configuration.DeviceRegistry.Zones.OrderBy(zone => zone.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            Zones.Add(zone);
        }

        Profiles.Clear();
        foreach (var profile in configuration.DeviceRegistry.DisplayProfiles.OrderBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            Profiles.Add(profile);
        }

        ProfileOptions.Clear();
        foreach (var profile in Profiles)
        {
            ProfileOptions.Add(new ZoneOptionViewModel(profile.DisplayProfileId, profile.Name, profile.IsEnabled));
        }

        if (SelectedProfileForZone is null || ProfileOptions.All(option => option.ZoneId != SelectedProfileForZone.ZoneId))
        {
            SelectedProfileForZone = ProfileOptions.FirstOrDefault();
        }

        OnPropertyChanged(nameof(HasProfiles));
    }

    private static string CreateSlug(string label, IEnumerable<string> existingValues)
    {
        var slug = new string(label
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "item";
        }

        var existing = new HashSet<string>(existingValues, StringComparer.OrdinalIgnoreCase);
        var candidate = slug;
        var suffix = 2;
        while (existing.Contains(candidate))
        {
            candidate = $"{slug}-{suffix++}";
        }

        return candidate;
    }

    private void OnConfigurationChanged(AppConfiguration configuration)
    {
        ApplyConfiguration(configuration);
    }

    public override void Dispose()
    {
        _configurationSession.ConfigurationChanged -= OnConfigurationChanged;
    }
}
