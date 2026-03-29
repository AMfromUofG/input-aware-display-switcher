namespace RawInputPrototype.RawInput;

internal sealed class SetupApiDeviceMetadata
{
    public static SetupApiDeviceMetadata CreateUnavailable(string normalizedPath, string reason)
    {
        return new SetupApiDeviceMetadata
        {
            NormalizedDeviceInterfacePath = normalizedPath,
            DeviceInstanceId = string.Empty,
            FriendlyName = string.Empty,
            DeviceDescription = string.Empty,
            DeviceClass = string.Empty,
            Manufacturer = string.Empty,
            EnumeratorName = string.Empty,
            LocationInformation = string.Empty,
            HardwareIds = [],
            LookupStatus = reason
        };
    }

    public required string NormalizedDeviceInterfacePath { get; init; }

    public required string DeviceInstanceId { get; init; }

    public required string FriendlyName { get; init; }

    public required string DeviceDescription { get; init; }

    public required string DeviceClass { get; init; }

    public required string Manufacturer { get; init; }

    public required string EnumeratorName { get; init; }

    public required string LocationInformation { get; init; }

    public required IReadOnlyList<string> HardwareIds { get; init; }

    public required string LookupStatus { get; init; }

    public string DisplayName => !string.IsNullOrWhiteSpace(FriendlyName)
        ? FriendlyName
        : DeviceDescription;

    public string HardwareIdsText => HardwareIds.Count == 0
        ? string.Empty
        : string.Join(" | ", HardwareIds);

    public string PrimaryHardwareId => HardwareIds.FirstOrDefault() ?? string.Empty;
}
