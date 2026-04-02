namespace InputAwareDisplaySwitcher.Core.Domain.Diagnostics;

public static class DiagnosticEventTypes
{
    public const string ApplicationStarted = "application.started";
    public const string ApplicationStartupFailed = "application.startup_failed";
    public const string ConfigurationPathSelected = "application.configuration_path_selected";
    public const string ShellInitialized = "application.shell_initialized";
    public const string ConfigurationLoaded = "configuration.loaded";
    public const string ConfigurationMissingFile = "configuration.missing_file";
    public const string ConfigurationLoadFailed = "configuration.load_failed";
    public const string ConfigurationSaved = "configuration.saved";
    public const string ConfigurationSaveFailed = "configuration.save_failed";
    public const string SwitchingControllerStarted = "switching.controller_started";
    public const string SwitchingControllerStopped = "switching.controller_stopped";
    public const string InputActivityDetected = "input.activity_detected";
    public const string RuntimeStateLoaded = "switching.runtime_state_loaded";
    public const string DeviceResolutionCompleted = "switching.device_resolution_completed";
    public const string SwitchDecisionEvaluated = "switching.decision_evaluated";
    public const string SwitchBlocked = "switching.blocked";
    public const string SwitchNoAction = "switching.no_action";
    public const string SwitchAttempted = "switching.attempted";
    public const string SwitchSucceeded = "switching.succeeded";
    public const string SwitchFailed = "switching.failed";
    public const string SwitchNotApplied = "switching.not_applied";
    public const string RuntimeStateUpdated = "switching.runtime_state_updated";
}
