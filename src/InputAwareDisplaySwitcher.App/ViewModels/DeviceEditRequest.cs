using InputAwareDisplaySwitcher.Core.Application;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed record DeviceEditRequest
{
    public required DeviceManagementEntry Entry { get; init; }

    public required string FriendlyName { get; init; }

    public string? AssignedZoneId { get; init; }
}
