using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace RawInputPrototype.RawInput;

internal sealed class DeviceInfoProvider
{
    private static readonly Regex VidPidRegex = new(@"VID_([0-9A-F]{4}).*PID_([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public IReadOnlyList<RawInputDeviceInfo> GetCurrentDevices()
    {
        var devices = new List<RawInputDeviceInfo>();
        uint deviceCount = 0;

        var countResult = RawInputInterop.GetRawInputDeviceList(
            nint.Zero,
            ref deviceCount,
            (uint)Marshal.SizeOf<RawInputInterop.RAWINPUTDEVICELIST>());

        if (countResult == uint.MaxValue)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (deviceCount == 0)
        {
            return devices;
        }

        var elementSize = Marshal.SizeOf<RawInputInterop.RAWINPUTDEVICELIST>();
        var buffer = Marshal.AllocHGlobal((int)(deviceCount * elementSize));

        try
        {
            var listResult = RawInputInterop.GetRawInputDeviceList(
                buffer,
                ref deviceCount,
                (uint)elementSize);

            if (listResult == uint.MaxValue)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            for (var index = 0; index < deviceCount; index++)
            {
                var current = buffer + (index * elementSize);
                var deviceEntry = Marshal.PtrToStructure<RawInputInterop.RAWINPUTDEVICELIST>(current);
                var deviceType = ToDeviceType(deviceEntry.dwType);

                if (deviceType is RawInputDeviceType.Keyboard or RawInputDeviceType.Mouse)
                {
                    devices.Add(TryGetDeviceInfo(deviceEntry.hDevice, deviceType)
                        ?? RawInputDeviceInfo.CreateFallback(deviceEntry.hDevice, deviceType, "Device metadata could not be resolved."));
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return devices
            .OrderBy(device => device.DeviceType)
            .ThenBy(device => device.Identifier, StringComparer.OrdinalIgnoreCase)
            .ThenBy(device => device.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public RawInputDeviceInfo? TryGetDeviceInfo(nint deviceHandle, RawInputDeviceType fallbackType = RawInputDeviceType.Unknown)
    {
        if (deviceHandle == nint.Zero)
        {
            return RawInputDeviceInfo.CreateFallback(deviceHandle, fallbackType, "Windows reported this event without a device handle.");
        }

        try
        {
            var info = GetDeviceInfo(deviceHandle);
            var deviceType = ToDeviceType(info.dwType);
            var name = GetDeviceName(deviceHandle);
            var (vendorId, productId) = ParseVidPid(name, info);

            return new RawInputDeviceInfo
            {
                DeviceHandle = deviceHandle,
                DeviceType = deviceType == RawInputDeviceType.Unknown ? fallbackType : deviceType,
                DeviceName = string.IsNullOrWhiteSpace(name) ? "(device name unavailable)" : name,
                VendorId = vendorId,
                ProductId = productId,
                Identifier = BuildIdentifier(deviceType == RawInputDeviceType.Unknown ? fallbackType : deviceType, vendorId, productId, name),
                Details = BuildDetails(info)
            };
        }
        catch (Win32Exception)
        {
            return null;
        }
    }

    private static RawInputInterop.RID_DEVICE_INFO GetDeviceInfo(nint deviceHandle)
    {
        var info = new RawInputInterop.RID_DEVICE_INFO
        {
            cbSize = (uint)Marshal.SizeOf<RawInputInterop.RID_DEVICE_INFO>()
        };

        var size = info.cbSize;
        var result = RawInputInterop.GetRawInputDeviceInfo(deviceHandle, RawInputInterop.RIDI_DEVICEINFO, ref info, ref size);

        if (result == uint.MaxValue)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return info;
    }

    private static string GetDeviceName(nint deviceHandle)
    {
        uint size = 0;

        var queryResult = RawInputInterop.GetRawInputDeviceInfo(deviceHandle, RawInputInterop.RIDI_DEVICENAME, nint.Zero, ref size);
        if (queryResult == uint.MaxValue)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (size == 0)
        {
            return string.Empty;
        }

        var buffer = Marshal.AllocHGlobal((int)(size * sizeof(char)));

        try
        {
            var nameSize = size;
            var nameResult = RawInputInterop.GetRawInputDeviceInfo(deviceHandle, RawInputInterop.RIDI_DEVICENAME, buffer, ref nameSize);

            if (nameResult == uint.MaxValue)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return Marshal.PtrToStringUni(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static (string VendorId, string ProductId) ParseVidPid(string deviceName, RawInputInterop.RID_DEVICE_INFO info)
    {
        var match = VidPidRegex.Match(deviceName);
        if (match.Success)
        {
            return (match.Groups[1].Value.ToUpperInvariant(), match.Groups[2].Value.ToUpperInvariant());
        }

        if (info.dwType == RawInputInterop.RIM_TYPEHID)
        {
            return ($"{info.info.hid.dwVendorId:X4}", $"{info.info.hid.dwProductId:X4}");
        }

        return (string.Empty, string.Empty);
    }

    private static string BuildIdentifier(RawInputDeviceType deviceType, string vendorId, string productId, string deviceName)
    {
        if (!string.IsNullOrWhiteSpace(vendorId) || !string.IsNullOrWhiteSpace(productId))
        {
            return $"{deviceType switch
            {
                RawInputDeviceType.Keyboard => "Keyboard",
                RawInputDeviceType.Mouse => "Mouse",
                _ => "Device"
            }} VID_{vendorId} PID_{productId}";
        }

        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            var shortenedName = deviceName.Replace('#', '\\');
            return shortenedName.Length <= 80 ? shortenedName : shortenedName[..80] + "...";
        }

        return "No identifier available";
    }

    private static string BuildDetails(RawInputInterop.RID_DEVICE_INFO info)
    {
        return info.dwType switch
        {
            RawInputInterop.RIM_TYPEKEYBOARD => $"Type {info.info.keyboard.dwType}, subtype {info.info.keyboard.dwSubType}, mode {info.info.keyboard.dwKeyboardMode}, total keys {info.info.keyboard.dwNumberOfKeysTotal}, function keys {info.info.keyboard.dwNumberOfFunctionKeys}",
            RawInputInterop.RIM_TYPEMOUSE => $"{info.info.mouse.dwNumberOfButtons} buttons, sample rate {info.info.mouse.dwSampleRate}, horizontal wheel {info.info.mouse.fHasHorizontalWheel}",
            RawInputInterop.RIM_TYPEHID => $"Usage page 0x{info.info.hid.usUsagePage:X4}, usage 0x{info.info.hid.usUsage:X4}, version 0x{info.info.hid.dwVersionNumber:X4}",
            _ => "Unknown raw input device type."
        };
    }

    private static RawInputDeviceType ToDeviceType(uint rawType)
    {
        return rawType switch
        {
            RawInputInterop.RIM_TYPEMOUSE => RawInputDeviceType.Mouse,
            RawInputInterop.RIM_TYPEKEYBOARD => RawInputDeviceType.Keyboard,
            RawInputInterop.RIM_TYPEHID => RawInputDeviceType.Hid,
            _ => RawInputDeviceType.Unknown
        };
    }
}
