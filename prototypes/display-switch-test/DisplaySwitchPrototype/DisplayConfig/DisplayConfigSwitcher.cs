namespace DisplaySwitchPrototype.DisplayConfig;

internal sealed class DisplayConfigSwitcher
{
    public DisplaySwitchAttemptResult ApplyTopology(DisplaySwitchAction action)
    {
        var topologyFlag = action switch
        {
            DisplaySwitchAction.InternalOnly => SetDisplayConfigFlags.TopologyInternal,
            DisplaySwitchAction.ExternalOnly => SetDisplayConfigFlags.TopologyExternal,
            DisplaySwitchAction.Extend => SetDisplayConfigFlags.TopologyExtend,
            DisplaySwitchAction.Clone => SetDisplayConfigFlags.TopologyClone,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown display switch action.")
        };

        var validationFlags = (uint)(
            SetDisplayConfigFlags.Validate |
            topologyFlag);

        var applyFlags = (uint)(
            SetDisplayConfigFlags.Apply |
            SetDisplayConfigFlags.SaveToDatabase |
            topologyFlag);

        return ExecuteValidatedCall(
            apiPath: "Database topology flag call",
            validationFlags: validationFlags,
            applyFlags: applyFlags,
            pathArray: null,
            modeInfoArray: null);
    }

    public DisplaySwitchAttemptResult RestoreSnapshot(DisplaySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var paths = (DISPLAYCONFIG_PATH_INFO[])snapshot.RawPaths.Clone();
        var modes = (DISPLAYCONFIG_MODE_INFO[])snapshot.RawModes.Clone();

        var validationFlags = (uint)(
            SetDisplayConfigFlags.UseSuppliedDisplayConfig |
            SetDisplayConfigFlags.AllowChanges |
            SetDisplayConfigFlags.Validate);

        var applyFlags = (uint)(
            SetDisplayConfigFlags.UseSuppliedDisplayConfig |
            SetDisplayConfigFlags.Apply |
            SetDisplayConfigFlags.AllowChanges |
            SetDisplayConfigFlags.SaveToDatabase);

        return ExecuteValidatedCall(
            apiPath: "Supplied captured path/mode restore call",
            validationFlags: validationFlags,
            applyFlags: applyFlags,
            pathArray: paths,
            modeInfoArray: modes);
    }

    private static DisplaySwitchAttemptResult ExecuteValidatedCall(
        string apiPath,
        uint validationFlags,
        uint applyFlags,
        DISPLAYCONFIG_PATH_INFO[]? pathArray,
        DISPLAYCONFIG_MODE_INFO[]? modeInfoArray)
    {
        var pathCount = pathArray is null ? 0u : (uint)pathArray.Length;
        var modeCount = modeInfoArray is null ? 0u : (uint)modeInfoArray.Length;

        var validationStatus = DisplayConfigInterop.SetDisplayConfig(
            pathCount,
            pathArray,
            modeCount,
            modeInfoArray,
            validationFlags);

        if (validationStatus != DisplayConfigInterop.Success)
        {
            return new DisplaySwitchAttemptResult
            {
                ApiPath = apiPath,
                ValidationFlags = validationFlags,
                ValidationStatusCode = validationStatus,
                ApplyFlags = applyFlags,
                ApplyStatusCode = null,
                Interpretation = DisplayConfigFormatter.FormatAttemptInterpretation(
                    apiPath,
                    validationStatus,
                    applyStatusCode: null)
            };
        }

        var applyStatus = DisplayConfigInterop.SetDisplayConfig(
            pathCount,
            pathArray,
            modeCount,
            modeInfoArray,
            applyFlags);

        return new DisplaySwitchAttemptResult
        {
            ApiPath = apiPath,
            ValidationFlags = validationFlags,
            ValidationStatusCode = validationStatus,
            ApplyFlags = applyFlags,
            ApplyStatusCode = applyStatus,
            Interpretation = DisplayConfigFormatter.FormatAttemptInterpretation(
                apiPath,
                validationStatus,
                applyStatus)
        };
    }
}
