using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using DisplaySwitchPrototype.DisplayConfig;

namespace DisplaySwitchPrototype;

public partial class MainWindow : Window
{
    private const int MaxLogEntries = 200;
    private static readonly TimeSpan RefreshDelayAfterSwitch = TimeSpan.FromMilliseconds(1200);

    private readonly ObservableCollection<DisplayPathSnapshot> _paths = [];
    private readonly ObservableCollection<DisplayLogEntry> _logEntries = [];
    private readonly DisplayConfigReader _reader = new();
    private readonly DisplayConfigSwitcher _switcher = new();

    private DisplaySnapshot? _currentSnapshot;
    private DisplaySnapshot? _capturedSnapshot;

    public MainWindow()
    {
        InitializeComponent();

        PathsDataGrid.ItemsSource = _paths;
        DiagnosticsDataGrid.ItemsSource = _logEntries;

        Loaded += MainWindow_Loaded;

        SnapshotTextBox.Text = "No display snapshot captured yet.";
        StatusTextBlock.Text = "Waiting for the window to finish loading before querying the active display configuration.";
        CapturedSnapshotTextBlock.Text = "Captured restore snapshot: none yet.";
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshSnapshotAsync(
            actionLabel: "Initial Snapshot",
            logOnSuccess: false,
            logOnFailure: true);

        if (_currentSnapshot is not null && _capturedSnapshot is null)
        {
            _capturedSnapshot = _currentSnapshot.Clone();
            UpdateCapturedSnapshotSummary();
            AppendLog(
                action: "Capture Current Snapshot",
                status: "Success",
                apiPath: "Captured active snapshot",
                flags: "n/a",
                statusCode: 0,
                details: $"Captured the startup display state for later restore. {_capturedSnapshot.TopologySummary}");
        }
    }

    private async Task RefreshSnapshotAsync(string actionLabel, bool logOnSuccess, bool logOnFailure)
    {
        try
        {
            var snapshot = _reader.ReadCurrentSnapshot();
            _currentSnapshot = snapshot;
            UpdateSnapshotUi(snapshot);

            if (logOnSuccess)
            {
                AppendLog(
                    action: actionLabel,
                    status: "Success",
                    statusCode: 0,
                    details: $"Snapshot refreshed. {snapshot.TopologySummary}");
            }
        }
        catch (Win32Exception exception)
        {
            var statusCode = exception.NativeErrorCode;
            UpdateStatus($"Failed to query the active display configuration: {exception.Message}");

            if (logOnFailure)
            {
                AppendLog(
                    action: actionLabel,
                    status: "Failure",
                    statusCode: statusCode,
                    details: $"Snapshot refresh failed. {DisplayConfigFormatter.FormatStatusCode(statusCode)}");
            }
        }
        catch (Exception exception)
        {
            UpdateStatus($"Unexpected error while reading display state: {exception.Message}");

            if (logOnFailure)
            {
                AppendLog(
                    action: actionLabel,
                    status: "Failure",
                    statusCode: -1,
                    details: $"Snapshot refresh failed unexpectedly: {exception.Message}");
            }
        }

        await Task.CompletedTask;
    }

    private async Task ExecuteTopologyActionAsync(DisplaySwitchAction action)
    {
        var label = DisplayConfigFormatter.GetActionLabel(action);
        ButtonsPanel.IsEnabled = false;
        UpdateStatus($"{label}: validating the database-topology SetDisplayConfig call before attempting apply.");

        try
        {
            var result = _switcher.ApplyTopology(action);
            await Task.Delay(RefreshDelayAfterSwitch);

            DisplaySnapshot? refreshedSnapshot = null;
            string refreshSummary;

            try
            {
                refreshedSnapshot = _reader.ReadCurrentSnapshot();
                _currentSnapshot = refreshedSnapshot;
                UpdateSnapshotUi(refreshedSnapshot);
                refreshSummary = $"Post-action snapshot: {refreshedSnapshot.TopologySummary}";
            }
            catch (Win32Exception refreshException)
            {
                refreshSummary = $"Post-action refresh failed: {DisplayConfigFormatter.FormatStatusCode(refreshException.NativeErrorCode)}";
            }

            AppendLog(
                action: label,
                status: result.Success ? "Success" : "Failure",
                apiPath: result.ApiPath,
                flags: $"Validate: {DisplayConfigFormatter.FormatFlagSummary(result.ValidationFlags)} | Apply: {DisplayConfigFormatter.FormatFlagSummary(result.ApplyFlags)}",
                statusCode: result.FinalStatusCode,
                details:
                    $"Validation: {DisplayConfigFormatter.FormatStatusCode(result.ValidationStatusCode)}. " +
                    $"Apply: {(result.ApplyStatusCode.HasValue ? DisplayConfigFormatter.FormatStatusCode(result.ApplyStatusCode.Value) : "Not attempted because validation failed.")} " +
                    $"{result.Interpretation} {refreshSummary}");

            UpdateStatus(result.Success
                ? $"{label}: validation and apply both succeeded. {refreshSummary}"
                : $"{label}: {result.Interpretation}");
        }
        finally
        {
            ButtonsPanel.IsEnabled = true;
        }
    }

    private async Task ExecuteRestoreAsync()
    {
        if (_capturedSnapshot is null)
        {
            UpdateStatus("No captured snapshot is available yet. Capture the current state before trying to restore it.");
            AppendLog(
                action: "Restore Captured Snapshot",
                status: "Unavailable",
                apiPath: "Supplied captured path/mode restore call",
                flags: "n/a",
                statusCode: 0,
                details: "Restore was skipped because no snapshot has been captured yet.");
            return;
        }

        ButtonsPanel.IsEnabled = false;
        UpdateStatus("Restore Captured Snapshot: validating the supplied captured path/mode restore call before attempting apply.");

        try
        {
            var result = _switcher.RestoreSnapshot(_capturedSnapshot);
            await Task.Delay(RefreshDelayAfterSwitch);

            string refreshSummary;

            try
            {
                var refreshedSnapshot = _reader.ReadCurrentSnapshot();
                _currentSnapshot = refreshedSnapshot;
                UpdateSnapshotUi(refreshedSnapshot);
                refreshSummary = $"Post-action snapshot: {refreshedSnapshot.TopologySummary}";
            }
            catch (Win32Exception refreshException)
            {
                refreshSummary = $"Post-action refresh failed: {DisplayConfigFormatter.FormatStatusCode(refreshException.NativeErrorCode)}";
            }

            AppendLog(
                action: "Restore Captured Snapshot",
                status: result.Success ? "Success" : "Failure",
                apiPath: result.ApiPath,
                flags: $"Validate: {DisplayConfigFormatter.FormatFlagSummary(result.ValidationFlags)} | Apply: {DisplayConfigFormatter.FormatFlagSummary(result.ApplyFlags)}",
                statusCode: result.FinalStatusCode,
                details:
                    $"Validation: {DisplayConfigFormatter.FormatStatusCode(result.ValidationStatusCode)}. " +
                    $"Apply: {(result.ApplyStatusCode.HasValue ? DisplayConfigFormatter.FormatStatusCode(result.ApplyStatusCode.Value) : "Not attempted because validation failed.")} " +
                    $"{result.Interpretation} {refreshSummary}");

            UpdateStatus(result.Success
                ? $"Restore Captured Snapshot: validation and apply both succeeded. {refreshSummary}"
                : $"Restore Captured Snapshot: {result.Interpretation}");
        }
        finally
        {
            ButtonsPanel.IsEnabled = true;
        }
    }

    private void UpdateSnapshotUi(DisplaySnapshot snapshot)
    {
        _paths.Clear();

        foreach (var path in snapshot.Paths)
        {
            _paths.Add(path);
        }

        SnapshotTextBox.Text = DisplayConfigFormatter.FormatSnapshot(snapshot);
        UpdateStatus($"Current snapshot refreshed. {snapshot.TopologySummary} Log entries: {_logEntries.Count}/{MaxLogEntries}.");
        UpdateCapturedSnapshotSummary();
    }

    private void UpdateCapturedSnapshotSummary()
    {
        CapturedSnapshotTextBlock.Text = _capturedSnapshot is null
            ? "Captured restore snapshot: none yet."
            : $"Captured restore snapshot: {_capturedSnapshot.CapturedAt:yyyy-MM-dd HH:mm:ss} | {_capturedSnapshot.TopologySummary}";
    }

    private void UpdateStatus(string message)
    {
        StatusTextBlock.Text = message;
    }

    private void AppendLog(string action, string status, int statusCode, string details)
    {
        AppendLog(action, status, "Snapshot read", "n/a", statusCode, details);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshSnapshotAsync(
            actionLabel: "Refresh Current Display State",
            logOnSuccess: true,
            logOnFailure: true);
    }

    private void CaptureSnapshotButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSnapshot is null)
        {
            UpdateStatus("Capture skipped because there is no current display snapshot yet. Refresh first.");
            AppendLog(
                action: "Capture Current Snapshot",
                status: "Unavailable",
                apiPath: "Captured active snapshot",
                flags: "n/a",
                statusCode: 0,
                details: "Capture was skipped because no current snapshot was available.");
            return;
        }

        _capturedSnapshot = _currentSnapshot.Clone();
        UpdateCapturedSnapshotSummary();
        UpdateStatus($"Captured the current display state for restore. {_capturedSnapshot.TopologySummary}");
        AppendLog(
            action: "Capture Current Snapshot",
            status: "Success",
            apiPath: "Captured active snapshot",
            flags: "n/a",
            statusCode: 0,
            details: $"Captured {_capturedSnapshot.PathCount} active path(s) and {_capturedSnapshot.ModeCount} mode entry(ies) for restore.");
    }

    private async void RestoreSnapshotButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteRestoreAsync();
    }

    private async void InternalOnlyButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteTopologyActionAsync(DisplaySwitchAction.InternalOnly);
    }

    private async void ExternalOnlyButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteTopologyActionAsync(DisplaySwitchAction.ExternalOnly);
    }

    private async void ExtendButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteTopologyActionAsync(DisplaySwitchAction.Extend);
    }

    private async void CloneButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteTopologyActionAsync(DisplaySwitchAction.Clone);
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        _logEntries.Clear();
        UpdateStatus(_currentSnapshot is null
            ? "Diagnostics log cleared."
            : $"Diagnostics log cleared. {_currentSnapshot.TopologySummary}");
    }

    private void AppendLog(string action, string status, string apiPath, string flags, int statusCode, string details)
    {
        _logEntries.Add(new DisplayLogEntry
        {
            Timestamp = DateTime.Now,
            Action = action,
            ApiPath = apiPath,
            Flags = flags,
            Status = status,
            StatusCode = statusCode,
            Details = details
        });

        while (_logEntries.Count > MaxLogEntries)
        {
            _logEntries.RemoveAt(0);
        }

        if (_logEntries.Count > 0)
        {
            DiagnosticsDataGrid.ScrollIntoView(_logEntries[^1]);
        }
    }
}
