using InputAwareDisplaySwitcher.Core.Domain.Zones;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class ZoneOptionViewModel
{
    public static ZoneOptionViewModel CreateUnassigned()
    {
        return new ZoneOptionViewModel(null, "Unassigned", isEnabled: true);
    }

    public ZoneOptionViewModel(string? zoneId, string displayName, bool isEnabled)
    {
        ZoneId = zoneId;
        DisplayName = displayName;
        IsEnabled = isEnabled;
    }

    public ZoneOptionViewModel(ZoneDefinition zone)
    {
        ArgumentNullException.ThrowIfNull(zone);

        ZoneId = zone.ZoneId;
        DisplayName = zone.IsEnabled ? zone.Name : $"{zone.Name} (disabled)";
        IsEnabled = zone.IsEnabled;
    }

    public string? ZoneId { get; }

    public string DisplayName { get; }

    public bool IsEnabled { get; }
}
