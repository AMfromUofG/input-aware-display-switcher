using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using InputAwareDisplaySwitcher.Core.Domain.Devices;

namespace InputAwareDisplaySwitcher.Infrastructure.Windows.Input;

internal sealed class WindowsRawInputDeviceEnumerator
{
    private static readonly Regex VidPidRegex = new(@"VID_([0-9A-F]{4}).*PID_([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public IReadOnlyList<RuntimeDeviceObservation> GetCurrentDevices()
    {
        uint deviceCount = 0;
        var countResult = WindowsRawInputInterop.GetRawInputDeviceList(
            nint.Zero,
            ref deviceCount,
            (uint)Marshal.SizeOf<WindowsRawInputInterop.RawInputDeviceListEntry>());

        if (countResult == uint.MaxValue)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (deviceCount == 0)
        {
            return Array.Empty<RuntimeDeviceObservation>();
        }

        var elementSize = Marshal.SizeOf<WindowsRawInputInterop.RawInputDeviceListEntry>();
        var buffer = Marshal.AllocHGlobal((int)(deviceCount * elementSize));

        try
        {
            var listResult = WindowsRawInputInterop.GetRawInputDeviceList(
                buffer,
                ref deviceCount,
                (uint)elementSize);

            if (listResult == uint.MaxValue)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var devices = new List<RuntimeDeviceObservation>();
            for (var index = 0; index < deviceCount; index++)
            {
                var current = buffer + (index * elementSize);
                var deviceEntry = Marshal.PtrToStructure<WindowsRawInputInterop.RawInputDeviceListEntry>(current);
                var deviceKind = ToDeviceKind(deviceEntry.DeviceType);

                if (deviceKind == DeviceKind.Unknown)
                {
                    continue;
                }

                devices.Add(CreateObservation(deviceEntry.DeviceHandle, deviceKind));
            }

            return devices
                .OrderBy(device => device.DeviceKind)
                .ThenBy(device => device.FriendlyName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(device => device.NormalizedDevicePath, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static RuntimeDeviceObservation CreateObservation(nint deviceHandle, DeviceKind deviceKind)
    {
        var observedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            var deviceInfo = GetDeviceInfo(deviceHandle);
            var rawDevicePath = GetDeviceName(deviceHandle);
            var pathInfo = DevicePathInfo.Create(rawDevicePath);
            var metadata = SetupApiMetadataLookup.TryResolve(pathInfo);
            var (vendorId, productId) = ParseVidPid(rawDevicePath, deviceInfo);
            var friendlyName = FirstNonEmpty(
                metadata.FriendlyName,
                metadata.DeviceDescription,
                metadata.DeviceInstanceId,
                rawDevicePath);

            return new RuntimeDeviceObservation
            {
                SessionDeviceId = WindowsRawInputInterop.FormatHandle(deviceHandle),
                DeviceKind = deviceKind,
                RawDevicePath = NullIfWhiteSpace(rawDevicePath),
                NormalizedDevicePath = NullIfWhiteSpace(pathInfo.NormalizedDeviceInterfacePath),
                InstanceId = NullIfWhiteSpace(metadata.DeviceInstanceId),
                VendorId = NullIfWhiteSpace(vendorId),
                ProductId = NullIfWhiteSpace(productId),
                FriendlyName = NullIfWhiteSpace(friendlyName),
                ObservedAtUtc = observedAtUtc,
                LastSeenAtUtc = observedAtUtc,
                IsAvailableThisSession = true
            };
        }
        catch (Win32Exception)
        {
            return new RuntimeDeviceObservation
            {
                SessionDeviceId = WindowsRawInputInterop.FormatHandle(deviceHandle),
                DeviceKind = deviceKind,
                FriendlyName = $"{deviceKind} {WindowsRawInputInterop.FormatHandle(deviceHandle)}",
                ObservedAtUtc = observedAtUtc,
                LastSeenAtUtc = observedAtUtc,
                IsAvailableThisSession = true
            };
        }
    }

    private static WindowsRawInputInterop.RawInputDeviceInfo GetDeviceInfo(nint deviceHandle)
    {
        var info = new WindowsRawInputInterop.RawInputDeviceInfo
        {
            Size = (uint)Marshal.SizeOf<WindowsRawInputInterop.RawInputDeviceInfo>()
        };

        var size = info.Size;
        var result = WindowsRawInputInterop.GetRawInputDeviceInfo(
            deviceHandle,
            WindowsRawInputInterop.DeviceInfoCommand,
            ref info,
            ref size);

        if (result == uint.MaxValue)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return info;
    }

    private static string GetDeviceName(nint deviceHandle)
    {
        uint size = 0;
        var queryResult = WindowsRawInputInterop.GetRawInputDeviceInfo(
            deviceHandle,
            WindowsRawInputInterop.DeviceNameCommand,
            nint.Zero,
            ref size);

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
            var nameResult = WindowsRawInputInterop.GetRawInputDeviceInfo(
                deviceHandle,
                WindowsRawInputInterop.DeviceNameCommand,
                buffer,
                ref nameSize);

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

    private static (string? VendorId, string? ProductId) ParseVidPid(
        string rawDevicePath,
        WindowsRawInputInterop.RawInputDeviceInfo deviceInfo)
    {
        var match = VidPidRegex.Match(rawDevicePath);
        if (match.Success)
        {
            return (match.Groups[1].Value.ToUpperInvariant(), match.Groups[2].Value.ToUpperInvariant());
        }

        if (deviceInfo.DeviceType == WindowsRawInputInterop.RawInputHid)
        {
            return ($"{deviceInfo.Union.Hid.VendorId:X4}", $"{deviceInfo.Union.Hid.ProductId:X4}");
        }

        return (null, null);
    }

    private static DeviceKind ToDeviceKind(uint rawType)
    {
        return rawType switch
        {
            WindowsRawInputInterop.RawInputKeyboard => DeviceKind.Keyboard,
            WindowsRawInputInterop.RawInputMouse => DeviceKind.Mouse,
            _ => DeviceKind.Unknown
        };
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private sealed record DevicePathInfo(string RawDevicePath, string NormalizedDeviceInterfacePath)
    {
        public static DevicePathInfo Create(string rawDevicePath)
        {
            if (string.IsNullOrWhiteSpace(rawDevicePath))
            {
                return new DevicePathInfo(string.Empty, string.Empty);
            }

            if (rawDevicePath.StartsWith(@"\??\", StringComparison.Ordinal))
            {
                return new DevicePathInfo(rawDevicePath, @"\\?\" + rawDevicePath[4..]);
            }

            return new DevicePathInfo(rawDevicePath, rawDevicePath);
        }
    }

    private sealed record SetupApiMetadata(
        string DeviceInstanceId,
        string FriendlyName,
        string DeviceDescription);

    private static class SetupApiMetadataLookup
    {
        public static SetupApiMetadata TryResolve(DevicePathInfo pathInfo)
        {
            if (string.IsNullOrWhiteSpace(pathInfo.NormalizedDeviceInterfacePath))
            {
                return new SetupApiMetadata(string.Empty, string.Empty, string.Empty);
            }

            nint deviceInfoSet = nint.Zero;

            try
            {
                deviceInfoSet = WindowsSetupApiInterop.SetupDiCreateDeviceInfoList(nint.Zero, nint.Zero);
                if (deviceInfoSet == nint.Zero || deviceInfoSet == WindowsSetupApiInterop.InvalidHandleValue)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var interfaceData = new WindowsSetupApiInterop.DeviceInterfaceData
                {
                    Size = (uint)Marshal.SizeOf<WindowsSetupApiInterop.DeviceInterfaceData>()
                };

                if (!WindowsSetupApiInterop.SetupDiOpenDeviceInterface(
                    deviceInfoSet,
                    pathInfo.NormalizedDeviceInterfacePath,
                    0,
                    ref interfaceData))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var deviceInfoData = new WindowsSetupApiInterop.DeviceInfoData
                {
                    Size = (uint)Marshal.SizeOf<WindowsSetupApiInterop.DeviceInfoData>()
                };

                uint requiredSize = 0;
                var detailResult = WindowsSetupApiInterop.SetupDiGetDeviceInterfaceDetail(
                    deviceInfoSet,
                    ref interfaceData,
                    nint.Zero,
                    0,
                    ref requiredSize,
                    ref deviceInfoData);

                if (!detailResult)
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    if (errorCode != WindowsSetupApiInterop.ErrorInsufficientBuffer)
                    {
                        throw new Win32Exception(errorCode);
                    }
                }

                return new SetupApiMetadata(
                    GetDeviceInstanceId(deviceInfoSet, deviceInfoData),
                    GetRegistryStringProperty(deviceInfoSet, deviceInfoData, WindowsSetupApiInterop.PropertyFriendlyName),
                    GetRegistryStringProperty(deviceInfoSet, deviceInfoData, WindowsSetupApiInterop.PropertyDeviceDescription));
            }
            catch (Win32Exception)
            {
                return new SetupApiMetadata(string.Empty, string.Empty, string.Empty);
            }
            finally
            {
                if (deviceInfoSet != nint.Zero && deviceInfoSet != WindowsSetupApiInterop.InvalidHandleValue)
                {
                    WindowsSetupApiInterop.SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
        }

        private static string GetDeviceInstanceId(nint deviceInfoSet, WindowsSetupApiInterop.DeviceInfoData deviceInfoData)
        {
            var buffer = new StringBuilder(256);
            if (WindowsSetupApiInterop.SetupDiGetDeviceInstanceId(
                deviceInfoSet,
                ref deviceInfoData,
                buffer,
                buffer.Capacity,
                out var requiredSize))
            {
                return buffer.ToString();
            }

            var errorCode = Marshal.GetLastWin32Error();
            if (errorCode != WindowsSetupApiInterop.ErrorInsufficientBuffer)
            {
                return string.Empty;
            }

            buffer = new StringBuilder(requiredSize + 1);
            if (!WindowsSetupApiInterop.SetupDiGetDeviceInstanceId(
                deviceInfoSet,
                ref deviceInfoData,
                buffer,
                buffer.Capacity,
                out _))
            {
                return string.Empty;
            }

            return buffer.ToString();
        }

        private static string GetRegistryStringProperty(
            nint deviceInfoSet,
            WindowsSetupApiInterop.DeviceInfoData deviceInfoData,
            uint property)
        {
            if (!TryGetRegistryProperty(deviceInfoSet, deviceInfoData, property, out var regType, out var buffer))
            {
                return string.Empty;
            }

            if (regType != WindowsSetupApiInterop.RegistryString || buffer.Length == 0)
            {
                return string.Empty;
            }

            return Encoding.Unicode.GetString(buffer).TrimEnd('\0').Trim();
        }

        private static bool TryGetRegistryProperty(
            nint deviceInfoSet,
            WindowsSetupApiInterop.DeviceInfoData deviceInfoData,
            uint property,
            out uint regType,
            out byte[] buffer)
        {
            regType = 0;
            buffer = [];
            uint requiredSize = 0;

            WindowsSetupApiInterop.SetupDiGetDeviceRegistryProperty(
                deviceInfoSet,
                ref deviceInfoData,
                property,
                out regType,
                [],
                0,
                out requiredSize);

            var errorCode = Marshal.GetLastWin32Error();
            if (errorCode == WindowsSetupApiInterop.ErrorInvalidData
                || errorCode == WindowsSetupApiInterop.ErrorNotFound
                || requiredSize == 0)
            {
                return false;
            }

            if (errorCode != WindowsSetupApiInterop.ErrorInsufficientBuffer)
            {
                return false;
            }

            buffer = new byte[requiredSize];
            if (!WindowsSetupApiInterop.SetupDiGetDeviceRegistryProperty(
                deviceInfoSet,
                ref deviceInfoData,
                property,
                out regType,
                buffer,
                requiredSize,
                out _))
            {
                buffer = [];
                return false;
            }

            return true;
        }
    }
}
