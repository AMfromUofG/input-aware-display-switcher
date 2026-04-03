using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;

namespace InputAwareDisplaySwitcher.Infrastructure.Windows.Input;

public sealed class WindowsInputDeviceSnapshotProvider : IInputDeviceSnapshotProvider
{
    private readonly WindowsRawInputDeviceEnumerator _enumerator = new();
    private readonly IDiagnosticsService _diagnostics;

    public WindowsInputDeviceSnapshotProvider(IDiagnosticsService? diagnostics = null)
    {
        _diagnostics = diagnostics ?? NullDiagnosticsService.Instance;
    }

    public Task<IReadOnlyList<RuntimeDeviceObservation>> GetCurrentDevicesAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<RuntimeDeviceObservation>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var devices = _enumerator.GetCurrentDevices();

                _diagnostics.Record(
                    DiagnosticCategories.Input,
                    DiagnosticEventTypes.DeviceSnapshotRefreshed,
                    "Current input devices were enumerated.",
                    details: new Dictionary<string, string?>
                    {
                        ["deviceCount"] = devices.Count.ToString()
                    });

                return devices;
            }
            catch (Exception exception)
            {
                _diagnostics.Record(
                    DiagnosticCategories.Input,
                    DiagnosticEventTypes.DeviceSnapshotRefreshFailed,
                    "Input device enumeration failed.",
                    DiagnosticSeverity.Warning,
                    new Dictionary<string, string?>
                    {
                        ["exceptionType"] = exception.GetType().Name,
                        ["exceptionMessage"] = exception.Message
                    });

                return Array.Empty<RuntimeDeviceObservation>();
            }
        }, cancellationToken);
    }
}
