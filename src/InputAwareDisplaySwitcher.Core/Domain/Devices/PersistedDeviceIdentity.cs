namespace InputAwareDisplaySwitcher.Core.Domain.Devices;

public sealed record PersistedDeviceIdentity
{
    public required string DeviceId { get; init; }

    public required string FriendlyName { get; init; }

    public DeviceKind DeviceKind { get; init; } = DeviceKind.Unknown;

    public required string PreferredPersistenceKey { get; init; }

    public DeviceIdentityEvidence IdentityEvidence { get; init; } = new();

    public string? AssignedZoneId { get; init; }

    public Dictionary<string, string> Metadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public DateTimeOffset? LastConfirmedAtUtc { get; init; }

    public bool IsEnabled { get; init; } = true;
}
