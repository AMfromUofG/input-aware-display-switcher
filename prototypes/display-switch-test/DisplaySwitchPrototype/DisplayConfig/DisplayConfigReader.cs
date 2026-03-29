using System.ComponentModel;

namespace DisplaySwitchPrototype.DisplayConfig;

internal sealed class DisplayConfigReader
{
    private const int MaxQueryAttempts = 3;

    public DisplaySnapshot ReadCurrentSnapshot()
    {
        var (paths, modes) = QueryConfig((uint)QueryDisplayConfigFlags.OnlyActivePaths);
        var databaseTopology = TryGetDatabaseTopologyId();
        var snapshotPaths = BuildPathSnapshots(paths, modes);

        return new DisplaySnapshot
        {
            CapturedAt = DateTime.Now,
            TopologySummary = BuildTopologySummary(paths),
            DatabaseTopologySummary = databaseTopology is null
                ? "Unavailable"
                : DisplayConfigFormatter.FormatTopology(databaseTopology.Value),
            PathCount = paths.Length,
            ModeCount = modes.Length,
            Paths = snapshotPaths,
            RawPaths = paths,
            RawModes = modes
        };
    }

    private static (DISPLAYCONFIG_PATH_INFO[] Paths, DISPLAYCONFIG_MODE_INFO[] Modes) QueryConfig(uint flags)
    {
        for (var attempt = 1; attempt <= MaxQueryAttempts; attempt++)
        {
            var sizeStatus = DisplayConfigInterop.GetDisplayConfigBufferSizes(
                flags,
                out var pathCount,
                out var modeCount);

            if (sizeStatus != DisplayConfigInterop.Success)
            {
                throw new Win32Exception(sizeStatus);
            }

            var paths = new DISPLAYCONFIG_PATH_INFO[(int)pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[(int)modeCount];

            var queryStatus = DisplayConfigInterop.QueryDisplayConfig(
                flags,
                ref pathCount,
                paths,
                ref modeCount,
                modes,
                IntPtr.Zero);

            if (queryStatus == DisplayConfigInterop.Success)
            {
                if (paths.Length != pathCount)
                {
                    Array.Resize(ref paths, (int)pathCount);
                }

                if (modes.Length != modeCount)
                {
                    Array.Resize(ref modes, (int)modeCount);
                }

                return (paths, modes);
            }

            if (queryStatus != DisplayConfigInterop.ErrorInsufficientBuffer || attempt == MaxQueryAttempts)
            {
                throw new Win32Exception(queryStatus);
            }
        }

        throw new InvalidOperationException("Display configuration query exhausted its retry budget.");
    }

    private static DISPLAYCONFIG_TOPOLOGY_ID? TryGetDatabaseTopologyId()
    {
        try
        {
            var sizeStatus = DisplayConfigInterop.GetDisplayConfigBufferSizes(
                (uint)QueryDisplayConfigFlags.DatabaseCurrent,
                out var pathCount,
                out var modeCount);

            if (sizeStatus != DisplayConfigInterop.Success)
            {
                return null;
            }

            var paths = new DISPLAYCONFIG_PATH_INFO[(int)pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[(int)modeCount];

            var status = DisplayConfigInterop.QueryDisplayConfigWithTopology(
                (uint)QueryDisplayConfigFlags.DatabaseCurrent,
                ref pathCount,
                paths,
                ref modeCount,
                modes,
                out var topologyId);

            return status == DisplayConfigInterop.Success ? topologyId : null;
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<DisplayPathSnapshot> BuildPathSnapshots(
        IReadOnlyList<DISPLAYCONFIG_PATH_INFO> paths,
        IReadOnlyList<DISPLAYCONFIG_MODE_INFO> modes)
    {
        var results = new List<DisplayPathSnapshot>(paths.Count);

        for (var index = 0; index < paths.Count; index++)
        {
            var path = paths[index];
            var sourceName = TryGetSourceName(path.sourceInfo);
            var targetName = TryGetTargetName(path.targetInfo);
            var sourceMode = TryGetSourceMode(path.sourceInfo, modes);
            var targetMode = TryGetTargetMode(path.targetInfo, modes);

            results.Add(new DisplayPathSnapshot
            {
                PathIndex = index + 1,
                SourceName = sourceName.DisplayName,
                TargetName = targetName.DisplayName,
                OutputTechnology = DisplayConfigFormatter.FormatOutputTechnology(path.targetInfo.outputTechnology),
                SourceMode = DisplayConfigFormatter.FormatSourceMode(sourceMode),
                TargetMode = DisplayConfigFormatter.FormatTargetMode(targetMode),
                RefreshRate = DisplayConfigFormatter.FormatRefreshRate(path.targetInfo.refreshRate, targetMode),
                TargetAvailability = path.targetInfo.targetAvailable ? "Yes" : "No",
                PathFlags = DisplayConfigFormatter.FormatPathFlags(path.flags),
                PathIdentifiers =
                    $"Source {DisplayConfigFormatter.FormatAdapterTargetId(path.sourceInfo.adapterId, path.sourceInfo.id)} | " +
                    $"Target {DisplayConfigFormatter.FormatAdapterTargetId(path.targetInfo.adapterId, path.targetInfo.id)}",
                SourceDevicePath = sourceName.DevicePath,
                TargetDevicePath = targetName.DevicePath
            });
        }

        return results;
    }

    private static string BuildTopologySummary(IReadOnlyList<DISPLAYCONFIG_PATH_INFO> paths)
    {
        if (paths.Count == 0)
        {
            return "No active display paths were reported.";
        }

        var distinctSources = paths
            .Select(path => DisplayConfigFormatter.FormatAdapterTargetId(path.sourceInfo.adapterId, path.sourceInfo.id))
            .Distinct(StringComparer.Ordinal)
            .Count();

        var distinctTargets = paths
            .Select(path => DisplayConfigFormatter.FormatAdapterTargetId(path.targetInfo.adapterId, path.targetInfo.id))
            .Distinct(StringComparer.Ordinal)
            .Count();

        string topologyLabel;

        if (paths.Count == 1)
        {
            topologyLabel = "Single active display path";
        }
        else if (distinctSources < paths.Count)
        {
            topologyLabel = "Clone / duplicate-like topology";
        }
        else
        {
            topologyLabel = "Extend-like topology";
        }

        return $"{topologyLabel}. Active paths: {paths.Count}. Distinct sources: {distinctSources}. Distinct targets: {distinctTargets}.";
    }

    private static DeviceNameInfo TryGetSourceName(DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo)
    {
        var request = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
        {
            header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                adapterId = sourceInfo.adapterId,
                id = sourceInfo.id,
                size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                type = DISPLAYCONFIG_DEVICE_INFO_TYPE.GetSourceName
            }
        };

        var status = DisplayConfigInterop.DisplayConfigGetDeviceInfo(ref request);

        return status == DisplayConfigInterop.Success
            ? new DeviceNameInfo(
                DisplayName: string.IsNullOrWhiteSpace(request.viewGdiDeviceName) ? $"Source {sourceInfo.id}" : request.viewGdiDeviceName,
                DevicePath: request.viewGdiDeviceName ?? string.Empty)
            : new DeviceNameInfo(
                DisplayName: $"Source {sourceInfo.id}",
                DevicePath: string.Empty);
    }

    private static DeviceNameInfo TryGetTargetName(DISPLAYCONFIG_PATH_TARGET_INFO targetInfo)
    {
        var request = new DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                adapterId = targetInfo.adapterId,
                id = targetInfo.id,
                size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                type = DISPLAYCONFIG_DEVICE_INFO_TYPE.GetTargetName
            }
        };

        var status = DisplayConfigInterop.DisplayConfigGetDeviceInfo(ref request);

        if (status == DisplayConfigInterop.Success)
        {
            var name = string.IsNullOrWhiteSpace(request.monitorFriendlyDeviceName)
                ? $"Target {targetInfo.id}"
                : request.monitorFriendlyDeviceName.Trim();

            return new DeviceNameInfo(name, request.monitorDevicePath?.Trim() ?? string.Empty);
        }

        return new DeviceNameInfo($"Target {targetInfo.id}", string.Empty);
    }

    private static DISPLAYCONFIG_SOURCE_MODE? TryGetSourceMode(
        DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo,
        IReadOnlyList<DISPLAYCONFIG_MODE_INFO> modes)
    {
        if (sourceInfo.modeInfoIdx == DisplayConfigInterop.DisplayConfigPathModeIdxInvalid
            || sourceInfo.modeInfoIdx >= modes.Count)
        {
            return null;
        }

        var modeInfo = modes[(int)sourceInfo.modeInfoIdx];
        return modeInfo.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.Source
            ? modeInfo.modeInfo.sourceMode
            : null;
    }

    private static DISPLAYCONFIG_TARGET_MODE? TryGetTargetMode(
        DISPLAYCONFIG_PATH_TARGET_INFO targetInfo,
        IReadOnlyList<DISPLAYCONFIG_MODE_INFO> modes)
    {
        if (targetInfo.modeInfoIdx == DisplayConfigInterop.DisplayConfigPathModeIdxInvalid
            || targetInfo.modeInfoIdx >= modes.Count)
        {
            return null;
        }

        var modeInfo = modes[(int)targetInfo.modeInfoIdx];
        return modeInfo.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.Target
            ? modeInfo.modeInfo.targetMode
            : null;
    }

    private sealed record DeviceNameInfo(string DisplayName, string DevicePath);
}
