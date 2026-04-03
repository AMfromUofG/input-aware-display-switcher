using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Configuration;
using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class RulesSettingsViewModel : SectionViewModelBase
{
    private const int MaximumSeconds = 86400;

    private readonly AppConfigurationSession _configurationSession;
    private readonly RelayCommand _saveCommand;
    private readonly RelayCommand _restoreDefaultsCommand;
    private AppPreferences _preferences;
    private bool _automationEnabled;
    private string _cooldownSecondsText = string.Empty;
    private string _recentActivityThresholdSecondsText = string.Empty;
    private PriorityModeOptionViewModel? _selectedPriorityMode;
    private bool _manualLockStopsSwitching;
    private bool _allowSameProfileRefresh;
    private string? _cooldownError;
    private string? _recentActivityThresholdError;
    private bool _isSaving;
    private string? _saveStatusMessage;
    private bool _saveStatusIsError;

    public RulesSettingsViewModel(
        AppConfigurationSession configurationSession,
        AppPreferences preferences,
        SwitchingPolicy policy)
        : base("Rules / Settings", "Switching policy, cooldowns, and operator preferences.")
    {
        _configurationSession = configurationSession ?? throw new ArgumentNullException(nameof(configurationSession));
        _preferences = preferences ?? new AppPreferences();
        _saveCommand = new RelayCommand(() => _ = SaveAsync(), CanSave);
        _restoreDefaultsCommand = new RelayCommand(RestoreDefaults, () => !IsSaving);
        PriorityModes =
        [
            new PriorityModeOptionViewModel(
                PriorityMode.MostRecentInputWins,
                "Most recent input wins",
                "Switch immediately to the latest mapped device, subject to cooldown and lock rules."),
            new PriorityModeOptionViewModel(
                PriorityMode.PreferHigherPriorityZone,
                "Keep a higher-priority zone active briefly",
                "If the current zone has a higher priority, keep it active during the recent activity window before allowing a lower-priority takeover.")
        ];

        ApplyPolicy(policy ?? new SwitchingPolicy());
        _configurationSession.ConfigurationChanged += OnConfigurationChanged;
    }

    public bool AutomationEnabled
    {
        get => _automationEnabled;
        set
        {
            if (!SetProperty(ref _automationEnabled, value))
            {
                return;
            }

            OnFormStateChanged();
        }
    }

    public string CooldownSecondsText
    {
        get => _cooldownSecondsText;
        set
        {
            if (!SetProperty(ref _cooldownSecondsText, value))
            {
                return;
            }

            CooldownError = ValidateSeconds(value, "Cooldown");
            OnFormStateChanged();
        }
    }

    public string RecentActivityThresholdSecondsText
    {
        get => _recentActivityThresholdSecondsText;
        set
        {
            if (!SetProperty(ref _recentActivityThresholdSecondsText, value))
            {
                return;
            }

            RecentActivityThresholdError = ValidateSeconds(value, "Recent activity window");
            OnFormStateChanged();
        }
    }

    public IReadOnlyList<PriorityModeOptionViewModel> PriorityModes { get; }

    public PriorityModeOptionViewModel? SelectedPriorityMode
    {
        get => _selectedPriorityMode;
        set
        {
            if (!SetProperty(ref _selectedPriorityMode, value))
            {
                return;
            }

            OnFormStateChanged();
        }
    }

    public bool ManualLockStopsSwitching
    {
        get => _manualLockStopsSwitching;
        set
        {
            if (!SetProperty(ref _manualLockStopsSwitching, value))
            {
                return;
            }

            OnFormStateChanged();
        }
    }

    public bool AllowSameProfileRefresh
    {
        get => _allowSameProfileRefresh;
        set
        {
            if (!SetProperty(ref _allowSameProfileRefresh, value))
            {
                return;
            }

            OnFormStateChanged();
        }
    }

    public string? CooldownError
    {
        get => _cooldownError;
        private set => SetProperty(ref _cooldownError, value);
    }

    public string? RecentActivityThresholdError
    {
        get => _recentActivityThresholdError;
        private set => SetProperty(ref _recentActivityThresholdError, value);
    }

    public bool HasValidationErrors => !string.IsNullOrWhiteSpace(CooldownError) || !string.IsNullOrWhiteSpace(RecentActivityThresholdError);

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (!SetProperty(ref _isSaving, value))
            {
                return;
            }

            NotifyCommandStateChanged();
        }
    }

    public string? SaveStatusMessage
    {
        get => _saveStatusMessage;
        private set => SetProperty(ref _saveStatusMessage, value);
    }

    public bool SaveStatusIsError
    {
        get => _saveStatusIsError;
        private set => SetProperty(ref _saveStatusIsError, value);
    }

    public RelayCommand SaveCommand => _saveCommand;

    public RelayCommand RestoreDefaultsCommand => _restoreDefaultsCommand;

    public bool CanSave()
    {
        return !IsSaving
            && !HasValidationErrors
            && SelectedPriorityMode is not null;
    }

    private void ApplyPolicy(SwitchingPolicy policy)
    {
        _manualLockStopsSwitching = policy.ManualLockStopsSwitching;
        _allowSameProfileRefresh = policy.AllowSameProfileRefresh;

        AutomationEnabled = policy.AutomationEnabled;
        CooldownSecondsText = ((int)policy.Cooldown.TotalSeconds).ToString();
        RecentActivityThresholdSecondsText = ((int)policy.RecentActivityThreshold.TotalSeconds).ToString();
        SelectedPriorityMode = PriorityModes.FirstOrDefault(option => option.Value == policy.PriorityMode) ?? PriorityModes[0];
        OnPropertyChanged(nameof(ManualLockStopsSwitching));
        OnPropertyChanged(nameof(AllowSameProfileRefresh));
        SaveStatusMessage = null;
        SaveStatusIsError = false;
    }

    private void RestoreDefaults()
    {
        ApplyPolicy(new SwitchingPolicy());
        SaveStatusMessage = "Defaults restored in the form. Choose Save settings to write them to config.";
        SaveStatusIsError = false;
    }

    private async Task SaveAsync()
    {
        if (!CanSave())
        {
            return;
        }

        IsSaving = true;
        SaveStatusMessage = "Saving rules to configuration...";
        SaveStatusIsError = false;

        try
        {
            var policy = BuildPolicy();
            await _configurationSession.UpdateAsync(current => current with
            {
                SwitchingPolicy = policy,
                Preferences = _preferences
            }).ConfigureAwait(true);

            SaveStatusMessage = "Rules and settings saved.";
            SaveStatusIsError = false;
        }
        catch (Exception exception)
        {
            SaveStatusMessage = $"Save failed: {exception.Message}";
            SaveStatusIsError = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private SwitchingPolicy BuildPolicy()
    {
        return new SwitchingPolicy
        {
            AutomationEnabled = AutomationEnabled,
            Cooldown = TimeSpan.FromSeconds(ParseSeconds(CooldownSecondsText)),
            RecentActivityThreshold = TimeSpan.FromSeconds(ParseSeconds(RecentActivityThresholdSecondsText)),
            PriorityMode = SelectedPriorityMode?.Value ?? PriorityMode.MostRecentInputWins,
            ManualLockStopsSwitching = ManualLockStopsSwitching,
            AllowSameProfileRefresh = AllowSameProfileRefresh
        };
    }

    private static string? ValidateSeconds(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return $"{label} is required.";
        }

        if (!int.TryParse(value, out var seconds))
        {
            return $"{label} must be a whole number of seconds.";
        }

        if (seconds < 0)
        {
            return $"{label} cannot be negative.";
        }

        if (seconds > MaximumSeconds)
        {
            return $"{label} must stay at or below {MaximumSeconds} seconds.";
        }

        return null;
    }

    private static int ParseSeconds(string value)
    {
        return int.TryParse(value, out var seconds)
            ? seconds
            : 0;
    }

    private void OnFormStateChanged()
    {
        OnPropertyChanged(nameof(HasValidationErrors));
        NotifyCommandStateChanged();
    }

    private void NotifyCommandStateChanged()
    {
        _saveCommand.NotifyCanExecuteChanged();
        _restoreDefaultsCommand.NotifyCanExecuteChanged();
    }

    private void OnConfigurationChanged(AppConfiguration configuration)
    {
        _preferences = configuration.Preferences;
        ApplyPolicy(configuration.SwitchingPolicy);
    }

    public override void Dispose()
    {
        _configurationSession.ConfigurationChanged -= OnConfigurationChanged;
    }
}
