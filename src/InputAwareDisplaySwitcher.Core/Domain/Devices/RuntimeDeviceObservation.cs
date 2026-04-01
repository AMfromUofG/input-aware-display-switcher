namespace InputAwareDisplaySwitcher.Core.Domain.Devices;

public sealed record RuntimeDeviceObservation
{
    public required string SessionDeviceId { get; init; }

    public DeviceKind DeviceKind { get; init; } = DeviceKind.Unknown;

    public string? RawDevicePath { get; init; }

    public string? NormalizedDevicePath { get; init; }

    public string? InstanceId { get; init; }

    public string? VendorId { get; init; }

    public string? ProductId { get; init; }

    public string? FriendlyName { get; init; }

    public DateTimeOffset ObservedAtUtc { get; init; }

    public DateTimeOffset? LastSeenAtUtc { get; init; }

    public bool IsAvailableThisSession { get; init; } = true;

    public IReadOnlyList<string> GetCandidatePersistenceKeys()
    {
        var keys = new List<string>();

        AddIfPresent(keys, BuildInstanceKey(InstanceId));
        AddIfPresent(keys, BuildPathKey(NormalizedDevicePath));
        AddIfPresent(keys, BuildRawPathKey(RawDevicePath));
        AddIfPresent(keys, BuildVidPidKey(DeviceKind, VendorId, ProductId));
        AddIfPresent(keys, BuildSessionKey(SessionDeviceId));

        return keys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static string? BuildInstanceKey(string? instanceId)
    {
        return string.IsNullOrWhiteSpace(instanceId)
            ? null
            : $"instance:{instanceId}";
    }

    internal static string? BuildPathKey(string? normalizedDevicePath)
    {
        return string.IsNullOrWhiteSpace(normalizedDevicePath)
            ? null
            : $"path:{normalizedDevicePath}";
    }

    internal static string? BuildRawPathKey(string? rawDevicePath)
    {
        return string.IsNullOrWhiteSpace(rawDevicePath)
            ? null
            : $"rawpath:{rawDevicePath}";
    }

    internal static string? BuildSessionKey(string? sessionDeviceId)
    {
        return string.IsNullOrWhiteSpace(sessionDeviceId)
            ? null
            : $"session:{sessionDeviceId}";
    }

    internal static string? BuildVidPidKey(DeviceKind deviceKind, string? vendorId, string? productId)
    {
        if (string.IsNullOrWhiteSpace(vendorId) && string.IsNullOrWhiteSpace(productId))
        {
            return null;
        }

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(vendorId))
        {
            parts.Add($"VID_{vendorId}");
        }

        if (!string.IsNullOrWhiteSpace(productId))
        {
            parts.Add($"PID_{productId}");
        }

        return $"{deviceKind.ToString().ToLowerInvariant()}:{string.Join("/", parts)}";
    }

    private static void AddIfPresent(ICollection<string> keys, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            keys.Add(value);
        }
    }
}
