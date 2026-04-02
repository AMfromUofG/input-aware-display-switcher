using InputAwareDisplaySwitcher.Core.Domain.Devices;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class DevicesViewModel : SectionViewModelBase
{
    public DevicesViewModel(IReadOnlyList<PersistedDeviceIdentity> devices)
        : base("Devices", "Saved physical device identities and their zone assignments.")
    {
        Devices = devices;
    }

    public IReadOnlyList<PersistedDeviceIdentity> Devices { get; }
}
