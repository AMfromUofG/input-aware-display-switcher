using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Devices;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class DeviceRowViewModel : ObservableObject
{
    private readonly Action<DeviceEditRequest>? _onDeviceEdited;
    private readonly DeviceManagementEntry _entry;
    private string _friendlyName;
    private string? _assignedZoneId;

    public DeviceRowViewModel(DeviceManagementEntry entry, Action<DeviceEditRequest>? onDeviceEdited = null)
    {
        _entry = entry ?? throw new ArgumentNullException(nameof(entry));
        _friendlyName = entry.DisplayName;
        _assignedZoneId = entry.AssignedZoneId;
        _onDeviceEdited = onDeviceEdited;
    }

    public string EntryId => _entry.EntryId;

    public string FriendlyName
    {
        get => _friendlyName;
        set
        {
            var normalized = NormalizeFriendlyName(value);
            if (!SetProperty(ref _friendlyName, normalized))
            {
                return;
            }

            PublishEdit();
        }
    }

    public string DeviceKindDisplay => _entry.DeviceKind switch
    {
        DeviceKind.Keyboard => "Keyboard",
        DeviceKind.Mouse => "Mouse",
        DeviceKind.PointingDevice => "Pointing device",
        _ => "Unknown"
    };

    public bool IsAssigned => _assignedZoneId is not null && _entry.AssignmentState == DeviceAssignmentState.Assigned;

    public string AssignmentStatus
    {
        get
        {
            if (_assignedZoneId is null)
            {
                return "Unassigned";
            }

            return _entry.AssignmentState == DeviceAssignmentState.UnknownZone
                && string.Equals(_assignedZoneId, _entry.AssignedZoneId, StringComparison.OrdinalIgnoreCase)
                ? "Zone missing"
                : "Assigned";
        }
    }

    public string? AssignedZoneId
    {
        get => _assignedZoneId;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null : value;
            if (!SetProperty(ref _assignedZoneId, normalized))
            {
                return;
            }

            OnPropertyChanged(nameof(IsAssigned));
            OnPropertyChanged(nameof(AssignmentStatus));
            OnPropertyChanged(nameof(AssignedZoneDisplayName));
            PublishEdit();
        }
    }

    public string AssignedZoneDisplayName => _assignedZoneId is null
        ? "No zone assigned"
        : _entry.AssignedZoneName ?? _assignedZoneId;

    public string AvailabilityStatus => _entry.IsDetectedThisSession
        ? "Detected now"
        : "Remembered only";

    public bool IsDetectedThisSession => _entry.IsDetectedThisSession;

    public bool CanEdit => _entry.CanPersistEdits;

    public string StableIdentitySummary => _entry.StableIdentitySummary;

    public string IdentitySummary => StableIdentitySummary;

    public string MetadataSummary => _entry.MetadataSummary;

    public string EnabledState => _entry.IsEnabled ? "Enabled" : "Disabled";

    public string? PersistenceWarning => _entry.PersistenceWarning;

    public string LastSeenDisplay => _entry.LastSeenAtUtc.HasValue
        ? _entry.LastSeenAtUtc.Value.LocalDateTime.ToString("g")
        : "Not seen yet";

    private void PublishEdit()
    {
        if (!CanEdit)
        {
            return;
        }

        _onDeviceEdited?.Invoke(new DeviceEditRequest
        {
            Entry = _entry,
            FriendlyName = _friendlyName,
            AssignedZoneId = _assignedZoneId
        });
    }

    private string NormalizeFriendlyName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? _entry.DisplayName : value.Trim();
    }
}
