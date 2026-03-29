namespace DisplaySwitchPrototype.DisplayConfig;

internal enum DisplaySwitchAction
{
    InternalOnly,
    ExternalOnly,
    Extend,
    Clone
}

internal sealed class DisplaySnapshot
{
    public required DateTime CapturedAt { get; init; }

    public required string TopologySummary { get; init; }

    public required string DatabaseTopologySummary { get; init; }

    public required int PathCount { get; init; }

    public required int ModeCount { get; init; }

    public required IReadOnlyList<DisplayPathSnapshot> Paths { get; init; }

    public required DISPLAYCONFIG_PATH_INFO[] RawPaths { get; init; }

    public required DISPLAYCONFIG_MODE_INFO[] RawModes { get; init; }

    public DisplaySnapshot Clone()
    {
        return new DisplaySnapshot
        {
            CapturedAt = CapturedAt,
            TopologySummary = TopologySummary,
            DatabaseTopologySummary = DatabaseTopologySummary,
            PathCount = PathCount,
            ModeCount = ModeCount,
            Paths = Paths.Select(path => path with { }).ToArray(),
            RawPaths = (DISPLAYCONFIG_PATH_INFO[])RawPaths.Clone(),
            RawModes = (DISPLAYCONFIG_MODE_INFO[])RawModes.Clone()
        };
    }
}

internal sealed record DisplayPathSnapshot
{
    public required int PathIndex { get; init; }

    public required string SourceName { get; init; }

    public required string TargetName { get; init; }

    public required string OutputTechnology { get; init; }

    public required string SourceMode { get; init; }

    public required string TargetMode { get; init; }

    public required string RefreshRate { get; init; }

    public required string TargetAvailability { get; init; }

    public required string PathFlags { get; init; }

    public required string PathIdentifiers { get; init; }

    public required string SourceDevicePath { get; init; }

    public required string TargetDevicePath { get; init; }
}

internal sealed class DisplaySwitchAttemptResult
{
    public required string ApiPath { get; init; }

    public required uint ValidationFlags { get; init; }

    public required int ValidationStatusCode { get; init; }

    public required uint ApplyFlags { get; init; }

    public required int? ApplyStatusCode { get; init; }

    public required string Interpretation { get; init; }

    public bool ValidationSucceeded => ValidationStatusCode == DisplayConfigInterop.Success;

    public bool ApplyAttempted => ApplyStatusCode.HasValue;

    public int FinalStatusCode => ApplyStatusCode ?? ValidationStatusCode;

    public bool Success => ValidationSucceeded && ApplyStatusCode == DisplayConfigInterop.Success;
}

internal sealed class DisplayLogEntry
{
    public required DateTime Timestamp { get; init; }

    public required string Action { get; init; }

    public required string ApiPath { get; init; }

    public required string Flags { get; init; }

    public required string Status { get; init; }

    public required int StatusCode { get; init; }

    public required string Details { get; init; }

    public string TimestampText => Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");

    public string StatusCodeText => StatusCode.ToString("D");
}
