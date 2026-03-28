namespace RawInputPrototype.RawInput;

internal sealed class RawInputDeviceInfo
{
    public required nint DeviceHandle { get; init; }

    public required RawInputDeviceType DeviceType { get; init; }

    public required string DeviceName { get; init; }

    public required string VendorId { get; init; }

    public required string ProductId { get; init; }

    public required string Identifier { get; init; }

    public required string Details { get; init; }

    public string DeviceHandleText => RawInputInterop.FormatHandle(DeviceHandle);

    public string DeviceTypeText => DeviceType switch
    {
        RawInputDeviceType.Keyboard => "Keyboard",
        RawInputDeviceType.Mouse => "Mouse",
        RawInputDeviceType.Hid => "HID",
        _ => "Unknown"
    };

    public string DisplayLabel
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Identifier))
            {
                return Identifier;
            }

            return $"{DeviceTypeText}: {DeviceHandleText}";
        }
    }

    public static RawInputDeviceInfo CreateFallback(nint deviceHandle, RawInputDeviceType deviceType, string reason)
    {
        return new RawInputDeviceInfo
        {
            DeviceHandle = deviceHandle,
            DeviceType = deviceType,
            DeviceName = "(device metadata unavailable)",
            VendorId = string.Empty,
            ProductId = string.Empty,
            Identifier = "Metadata unavailable",
            Details = reason
        };
    }
}
