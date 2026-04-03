using InputAwareDisplaySwitcher.Core.Domain.Devices;

namespace InputAwareDisplaySwitcher.Core.Application;

public sealed record DeviceManagementEntry
{
    public required string EntryId { get; init; }

    public required string DisplayName { get; init; }

    public DeviceKind DeviceKind { get; init; } = DeviceKind.Unknown;

    public string? PersistedDeviceId { get; init; }

    public PersistedDeviceIdentity? PersistedDevice { get; init; }

    public RuntimeDeviceObservation? Observation { get; init; }

    public bool IsDetectedThisSession { get; init; }

    public bool IsAvailableThisSession { get; init; }

    public bool IsEnabled { get; init; } = true;

    public string? AssignedZoneId { get; init; }

    public string? AssignedZoneName { get; init; }

    public DeviceAssignmentState AssignmentState { get; init; } = DeviceAssignmentState.Unassigned;

    public string StableIdentitySummary { get; init; } = string.Empty;

    public string MetadataSummary { get; init; } = string.Empty;

    public DateTimeOffset? LastSeenAtUtc { get; init; }

    public bool CanPersistEdits { get; init; }

    public string? PersistenceWarning { get; init; }
}
