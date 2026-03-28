namespace RawInputPrototype.RawInput;

internal sealed class ParsedRawInputEvent
{
    public required DateTime Timestamp { get; init; }

    public required nint DeviceHandle { get; init; }

    public required RawInputDeviceType DeviceType { get; init; }

    public required string InputSource { get; init; }

    public required string Summary { get; init; }

    public string DeviceTypeText => DeviceType switch
    {
        RawInputDeviceType.Keyboard => "Keyboard",
        RawInputDeviceType.Mouse => "Mouse",
        RawInputDeviceType.Hid => "HID",
        _ => "Unknown"
    };
}

internal sealed class RawInputEvent
{
    public required DateTime Timestamp { get; init; }

    public required string EventType { get; init; }

    public required string InputSource { get; init; }

    public required string DeviceHandle { get; init; }

    public required string DeviceLabel { get; init; }

    public required string Summary { get; init; }

    public string TimestampText => Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
}

internal enum RawInputDeviceType
{
    Unknown = -1,
    Mouse = 0,
    Keyboard = 1,
    Hid = 2
}
