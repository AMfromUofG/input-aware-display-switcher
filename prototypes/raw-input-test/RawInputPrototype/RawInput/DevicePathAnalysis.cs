using System.Text.RegularExpressions;

namespace RawInputPrototype.RawInput;

internal sealed class DevicePathAnalysis
{
    private static readonly Regex CollectionRegex = new(@"COL(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static DevicePathAnalysis Empty { get; } = new()
    {
        RawDevicePath = string.Empty,
        NormalizedDeviceInterfacePath = string.Empty,
        TransportSegment = string.Empty,
        InstanceSegment = string.Empty,
        InterfaceClassGuid = string.Empty,
        CollectionSuffix = string.Empty,
        PathKind = "Unavailable"
    };

    public required string RawDevicePath { get; init; }

    public required string NormalizedDeviceInterfacePath { get; init; }

    public required string TransportSegment { get; init; }

    public required string InstanceSegment { get; init; }

    public required string InterfaceClassGuid { get; init; }

    public required string CollectionSuffix { get; init; }

    public required string PathKind { get; init; }

    public bool HasNormalizedPath => !string.IsNullOrWhiteSpace(NormalizedDeviceInterfacePath);

    public static DevicePathAnalysis Create(string rawDevicePath)
    {
        if (string.IsNullOrWhiteSpace(rawDevicePath))
        {
            return Empty;
        }

        var normalizedPath = NormalizeDevicePath(rawDevicePath);
        var segments = normalizedPath
            .Split('#', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var transportSegment = segments.Length > 1 ? segments[1] : string.Empty;
        var instanceSegment = segments.Length > 2 ? segments[2] : string.Empty;
        var interfaceClassGuid = segments.LastOrDefault(segment => segment.StartsWith('{') && segment.EndsWith('}')) ?? string.Empty;

        var collectionMatch = CollectionRegex.Match(transportSegment);
        var collectionSuffix = collectionMatch.Success
            ? $"COL{collectionMatch.Groups[1].Value}"
            : string.Empty;

        return new DevicePathAnalysis
        {
            RawDevicePath = rawDevicePath,
            NormalizedDeviceInterfacePath = normalizedPath,
            TransportSegment = transportSegment,
            InstanceSegment = instanceSegment,
            InterfaceClassGuid = interfaceClassGuid,
            CollectionSuffix = collectionSuffix,
            PathKind = DescribePathKind(normalizedPath)
        };
    }

    private static string NormalizeDevicePath(string rawDevicePath)
    {
        if (rawDevicePath.StartsWith(@"\??\", StringComparison.Ordinal))
        {
            return @"\\?\" + rawDevicePath[4..];
        }

        return rawDevicePath;
    }

    private static string DescribePathKind(string normalizedPath)
    {
        if (normalizedPath.StartsWith(@"\\?\HID#", StringComparison.OrdinalIgnoreCase))
        {
            return "HID interface path";
        }

        if (normalizedPath.StartsWith(@"\\?\ROOT#", StringComparison.OrdinalIgnoreCase))
        {
            return "Root-enumerated device path";
        }

        if (normalizedPath.StartsWith(@"\\?\ACPI#", StringComparison.OrdinalIgnoreCase))
        {
            return "ACPI device path";
        }

        return "Raw Input device path";
    }
}
