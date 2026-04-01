using System.ComponentModel;
using InputAwareDisplaySwitcher.Core.Abstractions;
using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Infrastructure.Windows.DisplayConfig;

public sealed class WindowsDisplaySwitcher : IDisplaySwitcher
{
    private const string HintKey = "windows.topology";

    public Task<SwitchExecutionResult> ApplyAsync(DisplayProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(SwitchExecutionResult.Unsupported(
                profile.DisplayProfileId,
                "Windows display configuration API",
                "The Windows display switcher can only run on Windows."));
        }

        if (!TryResolveTopology(profile, out var topologyFlag, out var topologyLabel, out var message))
        {
            return Task.FromResult(SwitchExecutionResult.Unsupported(
                profile.DisplayProfileId,
                "Windows display configuration API",
                message));
        }

        var validationFlags = (uint)(SetDisplayConfigFlags.Validate | topologyFlag);
        var applyFlags = (uint)(SetDisplayConfigFlags.Apply | SetDisplayConfigFlags.SaveToDatabase | topologyFlag);
        var executionPath = $"SetDisplayConfig database topology ({topologyLabel})";

        var validationStatus = DisplayConfigInterop.SetDisplayConfig(0, IntPtr.Zero, 0, IntPtr.Zero, validationFlags);
        if (validationStatus != DisplayConfigInterop.Success)
        {
            return Task.FromResult(SwitchExecutionResult.Failure(
                profile.DisplayProfileId,
                executionPath,
                $"Validation failed before apply. {FormatStatus(validationStatus)}",
                validationStatus));
        }

        var applyStatus = DisplayConfigInterop.SetDisplayConfig(0, IntPtr.Zero, 0, IntPtr.Zero, applyFlags);
        if (applyStatus != DisplayConfigInterop.Success)
        {
            return Task.FromResult(SwitchExecutionResult.Failure(
                profile.DisplayProfileId,
                executionPath,
                $"Validation succeeded but apply failed. {FormatStatus(applyStatus)}",
                applyStatus));
        }

        return Task.FromResult(SwitchExecutionResult.Succeeded(profile.DisplayProfileId, executionPath));
    }

    private static bool TryResolveTopology(
        DisplayProfile profile,
        out SetDisplayConfigFlags topologyFlag,
        out string topologyLabel,
        out string message)
    {
        if (profile.ImplementationHints.TryGetValue(HintKey, out var hintValue)
            && TryParseTopologyHint(hintValue, out topologyFlag, out topologyLabel))
        {
            message = string.Empty;
            return true;
        }

        switch (profile.IntentKind)
        {
            case DisplayProfileIntentKind.InternalOnly:
                topologyFlag = SetDisplayConfigFlags.TopologyInternal;
                topologyLabel = "Internal only";
                message = string.Empty;
                return true;
            case DisplayProfileIntentKind.ExternalOnly:
                topologyFlag = SetDisplayConfigFlags.TopologyExternal;
                topologyLabel = "External only";
                message = string.Empty;
                return true;
            case DisplayProfileIntentKind.Extend:
                topologyFlag = SetDisplayConfigFlags.TopologyExtend;
                topologyLabel = "Extend";
                message = string.Empty;
                return true;
            case DisplayProfileIntentKind.Clone:
                topologyFlag = SetDisplayConfigFlags.TopologyClone;
                topologyLabel = "Clone";
                message = string.Empty;
                return true;
            default:
                topologyFlag = default;
                topologyLabel = "Unsupported";
                message = $"Profile '{profile.Name}' uses logical intent '{profile.IntentKind}' and needs a '{HintKey}' implementation hint before Windows can apply it.";
                return false;
        }
    }

    private static bool TryParseTopologyHint(
        string? hintValue,
        out SetDisplayConfigFlags topologyFlag,
        out string topologyLabel)
    {
        switch (hintValue?.Trim().ToLowerInvariant())
        {
            case "internal":
            case "internalonly":
                topologyFlag = SetDisplayConfigFlags.TopologyInternal;
                topologyLabel = "Internal only";
                return true;
            case "external":
            case "externalonly":
                topologyFlag = SetDisplayConfigFlags.TopologyExternal;
                topologyLabel = "External only";
                return true;
            case "extend":
                topologyFlag = SetDisplayConfigFlags.TopologyExtend;
                topologyLabel = "Extend";
                return true;
            case "clone":
            case "duplicate":
                topologyFlag = SetDisplayConfigFlags.TopologyClone;
                topologyLabel = "Clone";
                return true;
            default:
                topologyFlag = default;
                topologyLabel = "Unsupported";
                return false;
        }
    }

    private static string FormatStatus(int statusCode)
    {
        if (statusCode == DisplayConfigInterop.Success)
        {
            return "ERROR_SUCCESS (0): the Windows display configuration API reported success.";
        }

        var symbolicName = statusCode switch
        {
            5 => "ERROR_ACCESS_DENIED",
            50 => "ERROR_NOT_SUPPORTED",
            87 => "ERROR_INVALID_PARAMETER",
            122 => "ERROR_INSUFFICIENT_BUFFER",
            1168 => "ERROR_NOT_FOUND",
            1610 => "ERROR_BAD_CONFIGURATION",
            _ => "WIN32_ERROR"
        };

        return $"{symbolicName} ({statusCode}): {new Win32Exception(statusCode).Message}";
    }
}
