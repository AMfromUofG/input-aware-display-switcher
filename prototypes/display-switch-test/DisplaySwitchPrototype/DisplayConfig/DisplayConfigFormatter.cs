using System.ComponentModel;
using System.Globalization;

namespace DisplaySwitchPrototype.DisplayConfig;

internal static class DisplayConfigFormatter
{
    public static string FormatSnapshot(DisplaySnapshot snapshot)
    {
        var activeTargets = snapshot.Paths
            .Select(path => path.TargetName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var lines = new List<string>
        {
            $"Captured: {snapshot.CapturedAt:yyyy-MM-dd HH:mm:ss.fff}",
            $"Topology summary: {snapshot.TopologySummary}",
            $"Windows database topology: {snapshot.DatabaseTopologySummary}",
            $"Active path count: {snapshot.PathCount}",
            $"Mode entry count: {snapshot.ModeCount}",
            $"Active targets: {(activeTargets.Length == 0 ? "None reported" : string.Join(", ", activeTargets))}",
            "View scope: QueryDisplayConfig(OnlyActivePaths). This snapshot reflects the current active topology, not a full inventory of inactive-but-attached displays.",
            string.Empty
        };

        if (snapshot.Paths.Count == 0)
        {
            lines.Add("No active display paths were returned by QueryDisplayConfig.");
            return string.Join(Environment.NewLine, lines);
        }

        foreach (var path in snapshot.Paths)
        {
            lines.Add($"Path {path.PathIndex}: {path.SourceName} -> {path.TargetName}");
            lines.Add($"  Output: {path.OutputTechnology}");
            lines.Add($"  Source mode: {path.SourceMode}");
            lines.Add($"  Target mode: {path.TargetMode}");
            lines.Add($"  Refresh: {path.RefreshRate}");
            lines.Add($"  Target available: {path.TargetAvailability}");
            lines.Add($"  Flags: {path.PathFlags}");
            lines.Add($"  Identifiers: {path.PathIdentifiers}");

            if (!string.IsNullOrWhiteSpace(path.TargetDevicePath))
            {
                lines.Add($"  Monitor device path: {path.TargetDevicePath}");
            }

            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines);
    }

    public static string GetActionLabel(DisplaySwitchAction action)
    {
        return action switch
        {
            DisplaySwitchAction.InternalOnly => "Attempt Internal / Primary Only",
            DisplaySwitchAction.ExternalOnly => "Attempt External / Secondary Only",
            DisplaySwitchAction.Extend => "Attempt Extend",
            DisplaySwitchAction.Clone => "Attempt Clone / Duplicate",
            _ => action.ToString()
        };
    }

    public static string FormatStatusCode(int statusCode)
    {
        if (statusCode == DisplayConfigInterop.Success)
        {
            return "ERROR_SUCCESS (0): the Windows display configuration API reported success.";
        }

        var name = statusCode switch
        {
            5 => "ERROR_ACCESS_DENIED",
            50 => "ERROR_NOT_SUPPORTED",
            87 => "ERROR_INVALID_PARAMETER",
            122 => "ERROR_INSUFFICIENT_BUFFER",
            1168 => "ERROR_NOT_FOUND",
            1610 => "ERROR_BAD_CONFIGURATION",
            _ => "WIN32_ERROR"
        };

        var message = new Win32Exception(statusCode).Message;
        return $"{name} ({statusCode}): {message}";
    }

    public static string FormatAttemptInterpretation(
        string apiPath,
        int validationStatusCode,
        int? applyStatusCode)
    {
        if (validationStatusCode != DisplayConfigInterop.Success)
        {
            return validationStatusCode switch
            {
                87 => $"Validation failed before apply. Windows rejected the requested {apiPath} call shape with ERROR_INVALID_PARAMETER, which suggests the current parameter/flag combination is not valid on this setup.",
                _ => $"Validation failed before apply. {FormatStatusCode(validationStatusCode)}"
            };
        }

        if (!applyStatusCode.HasValue)
        {
            return "Validation succeeded, but no apply call was attempted.";
        }

        if (applyStatusCode.Value == DisplayConfigInterop.Success)
        {
            return "Validation and apply both succeeded according to the Windows display configuration API.";
        }

        return applyStatusCode.Value switch
        {
            87 => $"Validation succeeded but apply failed with ERROR_INVALID_PARAMETER. This suggests the request still was not acceptable when Windows attempted to commit it.",
            _ => $"Validation succeeded, but apply failed. {FormatStatusCode(applyStatusCode.Value)}"
        };
    }

    public static string FormatFlagSummary(uint flags)
    {
        var parts = new List<string>();

        if ((flags & (uint)SetDisplayConfigFlags.Validate) != 0)
        {
            parts.Add("Validate");
        }

        if ((flags & (uint)SetDisplayConfigFlags.Apply) != 0)
        {
            parts.Add("Apply");
        }

        if ((flags & (uint)SetDisplayConfigFlags.SaveToDatabase) != 0)
        {
            parts.Add("SaveToDatabase");
        }

        if ((flags & (uint)SetDisplayConfigFlags.AllowChanges) != 0)
        {
            parts.Add("AllowChanges");
        }

        if ((flags & (uint)SetDisplayConfigFlags.UseSuppliedDisplayConfig) != 0)
        {
            parts.Add("UseSuppliedDisplayConfig");
        }

        if ((flags & (uint)SetDisplayConfigFlags.TopologySupplied) != 0)
        {
            parts.Add("TopologySupplied");
        }

        if ((flags & (uint)SetDisplayConfigFlags.TopologyInternal) != 0)
        {
            parts.Add("TopologyInternal");
        }

        if ((flags & (uint)SetDisplayConfigFlags.TopologyExternal) != 0)
        {
            parts.Add("TopologyExternal");
        }

        if ((flags & (uint)SetDisplayConfigFlags.TopologyExtend) != 0)
        {
            parts.Add("TopologyExtend");
        }

        if ((flags & (uint)SetDisplayConfigFlags.TopologyClone) != 0)
        {
            parts.Add("TopologyClone");
        }

        return parts.Count == 0
            ? "Flags: none"
            : $"Flags: {string.Join(", ", parts)}";
    }

    public static string FormatTopology(DISPLAYCONFIG_TOPOLOGY_ID topology)
    {
        return topology switch
        {
            DISPLAYCONFIG_TOPOLOGY_ID.Internal => "Internal",
            DISPLAYCONFIG_TOPOLOGY_ID.Clone => "Clone / Duplicate",
            DISPLAYCONFIG_TOPOLOGY_ID.Extend => "Extend",
            DISPLAYCONFIG_TOPOLOGY_ID.External => "External",
            _ => $"Unknown ({(uint)topology})"
        };
    }

    public static string FormatOutputTechnology(DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY technology)
    {
        return technology switch
        {
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Hd15 => "HD15 / VGA",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Svideo => "S-Video",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.CompositeVideo => "Composite",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.ComponentVideo => "Component",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Dvi => "DVI",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Hdmi => "HDMI",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Lvds => "LVDS",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DisplayPortExternal => "DisplayPort",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DisplayPortEmbedded => "Embedded DisplayPort",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.UdiExternal => "UDI External",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.UdiEmbedded => "UDI Embedded",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Miracast => "Miracast",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.IndirectWired => "Indirect Wired",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.IndirectVirtual => "Indirect Virtual",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Internal => "Internal",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Other => "Other / Unknown",
            _ => technology.ToString()
        };
    }

    public static string FormatSourceMode(DISPLAYCONFIG_SOURCE_MODE? mode)
    {
        if (mode is null)
        {
            return "Unavailable";
        }

        return $"{mode.Value.width}x{mode.Value.height} @ ({mode.Value.position.x},{mode.Value.position.y})";
    }

    public static string FormatTargetMode(DISPLAYCONFIG_TARGET_MODE? mode)
    {
        if (mode is null)
        {
            return "Unavailable";
        }

        var activeSize = mode.Value.targetVideoSignalInfo.activeSize;
        return activeSize.cx == 0 || activeSize.cy == 0
            ? "Unavailable"
            : $"{activeSize.cx}x{activeSize.cy}";
    }

    public static string FormatRefreshRate(
        DISPLAYCONFIG_RATIONAL rate,
        DISPLAYCONFIG_TARGET_MODE? targetMode)
    {
        if (rate.Denominator != 0 && rate.Numerator != 0)
        {
            return $"{(double)rate.Numerator / rate.Denominator:0.##} Hz";
        }

        if (targetMode is null)
        {
            return "Unknown";
        }

        var vSync = targetMode.Value.targetVideoSignalInfo.vSyncFreq;
        return vSync.Denominator == 0 || vSync.Numerator == 0
            ? "Unknown"
            : $"{(double)vSync.Numerator / vSync.Denominator:0.##} Hz";
    }

    public static string FormatPathFlags(uint flags)
    {
        var parts = new List<string>();

        if ((flags & DisplayConfigInterop.DisplayConfigPathActive) != 0)
        {
            parts.Add("Active");
        }

        if (parts.Count == 0)
        {
            parts.Add("None");
        }

        return string.Join(", ", parts);
    }

    public static string FormatAdapterTargetId(LUID adapterId, uint id)
    {
        return $"{adapterId.HighPart.ToString(CultureInfo.InvariantCulture)}:{adapterId.LowPart.ToString(CultureInfo.InvariantCulture)}:{id.ToString(CultureInfo.InvariantCulture)}";
    }
}
