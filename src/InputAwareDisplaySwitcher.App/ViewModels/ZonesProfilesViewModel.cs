using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Zones;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class ZonesProfilesViewModel : SectionViewModelBase
{
    public ZonesProfilesViewModel(
        IReadOnlyList<ZoneDefinition> zones,
        IReadOnlyList<DisplayProfile> profiles)
        : base("Zones / Profiles", "Logical zones and the display intents they resolve to.")
    {
        Zones = zones;
        Profiles = profiles;
    }

    public IReadOnlyList<ZoneDefinition> Zones { get; }

    public IReadOnlyList<DisplayProfile> Profiles { get; }
}
