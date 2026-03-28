using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using RawInputPrototype.RawInput;

namespace RawInputPrototype;

public partial class MainWindow : Window
{
    private const int MaxEventEntries = 300;

    private readonly ObservableCollection<RawInputDeviceInfo> _devices = [];
    private readonly ObservableCollection<RawInputEvent> _events = [];
    private readonly Dictionary<nint, RawInputDeviceInfo> _deviceCache = [];
    private readonly DeviceInfoProvider _deviceInfoProvider = new();

    private HwndSource? _windowSource;

    public MainWindow()
    {
        InitializeComponent();

        DevicesDataGrid.ItemsSource = _devices;
        EventsDataGrid.ItemsSource = _events;

        SourceInitialized += MainWindow_SourceInitialized;
        Closing += MainWindow_Closing;

        UpdateStatus("Waiting for window handle so Raw Input registration can start.");
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _windowSource = (HwndSource?)PresentationSource.FromVisual(this);

        if (_windowSource is null)
        {
            UpdateStatus("Unable to acquire the window handle needed for Raw Input registration.");
            return;
        }

        _windowSource.AddHook(WndProc);

        try
        {
            RawInputRegistration.RegisterKeyboardAndMouse(_windowSource.Handle);
            RefreshDeviceSnapshot();
            AppendSystemEvent("Raw Input registration succeeded. Keyboard and mouse activity will now be logged.");
        }
        catch (Win32Exception exception)
        {
            UpdateStatus($"Raw Input registration failed: {exception.Message}");
            AppendSystemEvent($"Raw Input registration failed: {exception.Message}");
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_windowSource is not null)
        {
            _windowSource.RemoveHook(WndProc);
        }
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        switch (msg)
        {
            case RawInputInterop.WM_INPUT:
                HandleRawInput(wParam, lParam);
                break;
            case RawInputInterop.WM_INPUT_DEVICE_CHANGE:
                HandleDeviceChange(wParam, lParam);
                break;
        }

        handled = false;
        return nint.Zero;
    }

    private void HandleRawInput(nint wParam, nint lParam)
    {
        if (!RawInputParser.TryReadEvent(lParam, wParam, out var parsedEvent, out var error))
        {
            AppendSystemEvent($"Failed to parse WM_INPUT: {error}");
            return;
        }

        if (parsedEvent is null)
        {
            AppendSystemEvent("Received WM_INPUT with no parsed event payload.");
            return;
        }

        AppendEvent(CreateDisplayEvent(parsedEvent));
    }

    private void HandleDeviceChange(nint wParam, nint lParam)
    {
        var deviceHandle = lParam;
        var changeSummary = wParam.ToInt64() switch
        {
            RawInputInterop.GIDC_ARRIVAL => "Device arrival detected.",
            RawInputInterop.GIDC_REMOVAL => "Device removal detected.",
            _ => $"Device change message received with code {wParam.ToInt64()}."
        };

        var deviceInfo = GetDeviceInfo(deviceHandle, RawInputDeviceType.Unknown);

        AppendEvent(new RawInputEvent
        {
            Timestamp = DateTime.Now,
            EventType = "Device",
            InputSource = "System",
            DeviceHandle = RawInputInterop.FormatHandle(deviceHandle),
            DeviceLabel = deviceInfo.DisplayLabel,
            Summary = changeSummary
        });

        RefreshDeviceSnapshot();
    }

    private RawInputEvent CreateDisplayEvent(ParsedRawInputEvent parsedEvent)
    {
        var deviceInfo = GetDeviceInfo(parsedEvent.DeviceHandle, parsedEvent.DeviceType);

        return new RawInputEvent
        {
            Timestamp = parsedEvent.Timestamp,
            EventType = parsedEvent.DeviceTypeText,
            InputSource = parsedEvent.InputSource,
            DeviceHandle = RawInputInterop.FormatHandle(parsedEvent.DeviceHandle),
            DeviceLabel = deviceInfo.DisplayLabel,
            Summary = parsedEvent.Summary
        };
    }

    private RawInputDeviceInfo GetDeviceInfo(nint deviceHandle, RawInputDeviceType deviceType)
    {
        if (_deviceCache.TryGetValue(deviceHandle, out var cached))
        {
            return cached;
        }

        RawInputDeviceInfo deviceInfo;

        if (deviceHandle == nint.Zero)
        {
            deviceInfo = RawInputDeviceInfo.CreateFallback(deviceHandle, deviceType, "Windows did not provide a device handle for this event.");
        }
        else
        {
            deviceInfo = _deviceInfoProvider.TryGetDeviceInfo(deviceHandle, deviceType)
                ?? RawInputDeviceInfo.CreateFallback(deviceHandle, deviceType, "Device metadata could not be resolved.");
        }

        _deviceCache[deviceHandle] = deviceInfo;
        return deviceInfo;
    }

    private void RefreshDeviceSnapshot()
    {
        try
        {
            var devices = _deviceInfoProvider.GetCurrentDevices();

            _devices.Clear();

            foreach (var device in devices)
            {
                _devices.Add(device);
                _deviceCache[device.DeviceHandle] = device;
            }

            UpdateStatus($"Listening for Raw Input events. Snapshot contains {_devices.Count} keyboard/mouse devices. Event log keeps the latest {MaxEventEntries} entries.");
        }
        catch (Win32Exception exception)
        {
            UpdateStatus($"Failed to refresh device snapshot: {exception.Message}");
            AppendSystemEvent($"Failed to refresh device snapshot: {exception.Message}");
        }
    }

    private void AppendSystemEvent(string summary)
    {
        AppendEvent(new RawInputEvent
        {
            Timestamp = DateTime.Now,
            EventType = "System",
            InputSource = "Window",
            DeviceHandle = RawInputInterop.FormatHandle(nint.Zero),
            DeviceLabel = "Prototype",
            Summary = summary
        });
    }

    private void AppendEvent(RawInputEvent entry)
    {
        _events.Add(entry);

        while (_events.Count > MaxEventEntries)
        {
            _events.RemoveAt(0);
        }

        if (_events.Count > 0)
        {
            EventsDataGrid.ScrollIntoView(_events[^1]);
        }

        UpdateStatus($"Listening for Raw Input events. Snapshot contains {_devices.Count} keyboard/mouse devices. Event log: {_events.Count}/{MaxEventEntries} entries.");
    }

    private void UpdateStatus(string message)
    {
        StatusTextBlock.Text = message;
    }

    private void RefreshDevicesButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshDeviceSnapshot();
        AppendSystemEvent("Device snapshot refreshed.");
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        _events.Clear();
        UpdateStatus($"Listening for Raw Input events. Snapshot contains {_devices.Count} keyboard/mouse devices. Event log cleared.");
    }

    private void CopyLogButton_Click(object sender, RoutedEventArgs e)
    {
        if (_events.Count == 0)
        {
            UpdateStatus("There are no event log entries to copy yet.");
            return;
        }

        var builder = new StringBuilder();

        foreach (var entry in _events)
        {
            builder.Append(entry.TimestampText)
                .Append('\t')
                .Append(entry.EventType)
                .Append('\t')
                .Append(entry.InputSource)
                .Append('\t')
                .Append(entry.DeviceHandle)
                .Append('\t')
                .Append(entry.DeviceLabel)
                .Append('\t')
                .Append(entry.Summary)
                .AppendLine();
        }

        try
        {
            Clipboard.SetText(builder.ToString());
            UpdateStatus($"Copied {_events.Count} event log entries to the clipboard.");
        }
        catch (Exception exception)
        {
            UpdateStatus($"Could not copy the event log to the clipboard: {exception.Message}");
        }
    }
}
