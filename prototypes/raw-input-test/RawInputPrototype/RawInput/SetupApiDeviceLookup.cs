using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace RawInputPrototype.RawInput;

internal sealed class SetupApiDeviceLookup
{
    public SetupApiDeviceMetadata TryResolve(DevicePathAnalysis pathAnalysis)
    {
        if (!pathAnalysis.HasNormalizedPath)
        {
            return SetupApiDeviceMetadata.CreateUnavailable(string.Empty, "Raw Input did not expose a usable device interface path.");
        }

        nint deviceInfoSet = nint.Zero;

        try
        {
            deviceInfoSet = SetupApiInterop.SetupDiCreateDeviceInfoList(nint.Zero, nint.Zero);
            if (deviceInfoSet == nint.Zero || deviceInfoSet == SetupApiInterop.InvalidHandleValue)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var deviceInterfaceData = new SetupApiInterop.SP_DEVICE_INTERFACE_DATA
            {
                cbSize = (uint)Marshal.SizeOf<SetupApiInterop.SP_DEVICE_INTERFACE_DATA>()
            };

            if (!SetupApiInterop.SetupDiOpenDeviceInterface(deviceInfoSet, pathAnalysis.NormalizedDeviceInterfacePath, 0, ref deviceInterfaceData))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var deviceInfoData = new SetupApiInterop.SP_DEVINFO_DATA
            {
                cbSize = (uint)Marshal.SizeOf<SetupApiInterop.SP_DEVINFO_DATA>()
            };

            uint requiredSize = 0;
            var detailResult = SetupApiInterop.SetupDiGetDeviceInterfaceDetail(
                deviceInfoSet,
                ref deviceInterfaceData,
                nint.Zero,
                0,
                ref requiredSize,
                ref deviceInfoData);

            if (!detailResult)
            {
                var errorCode = Marshal.GetLastWin32Error();
                if (errorCode != SetupApiInterop.ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new Win32Exception(errorCode);
                }
            }

            return new SetupApiDeviceMetadata
            {
                NormalizedDeviceInterfacePath = pathAnalysis.NormalizedDeviceInterfacePath,
                DeviceInstanceId = GetDeviceInstanceId(deviceInfoSet, deviceInfoData),
                FriendlyName = GetRegistryStringProperty(deviceInfoSet, deviceInfoData, SetupApiInterop.SPDRP_FRIENDLYNAME),
                DeviceDescription = GetRegistryStringProperty(deviceInfoSet, deviceInfoData, SetupApiInterop.SPDRP_DEVICEDESC),
                DeviceClass = GetRegistryStringProperty(deviceInfoSet, deviceInfoData, SetupApiInterop.SPDRP_CLASS),
                Manufacturer = GetRegistryStringProperty(deviceInfoSet, deviceInfoData, SetupApiInterop.SPDRP_MFG),
                EnumeratorName = GetRegistryStringProperty(deviceInfoSet, deviceInfoData, SetupApiInterop.SPDRP_ENUMERATOR_NAME),
                LocationInformation = GetRegistryStringProperty(deviceInfoSet, deviceInfoData, SetupApiInterop.SPDRP_LOCATION_INFORMATION),
                HardwareIds = GetRegistryMultiStringProperty(deviceInfoSet, deviceInfoData, SetupApiInterop.SPDRP_HARDWAREID),
                LookupStatus = "Resolved via SetupAPI device-interface lookup."
            };
        }
        catch (Win32Exception exception)
        {
            return SetupApiDeviceMetadata.CreateUnavailable(
                pathAnalysis.NormalizedDeviceInterfacePath,
                $"SetupAPI lookup failed: {exception.Message}");
        }
        finally
        {
            if (deviceInfoSet != nint.Zero && deviceInfoSet != SetupApiInterop.InvalidHandleValue)
            {
                SetupApiInterop.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }
    }

    private static string GetDeviceInstanceId(nint deviceInfoSet, SetupApiInterop.SP_DEVINFO_DATA deviceInfoData)
    {
        var buffer = new StringBuilder(256);

        if (SetupApiInterop.SetupDiGetDeviceInstanceId(deviceInfoSet, ref deviceInfoData, buffer, buffer.Capacity, out var requiredSize))
        {
            return buffer.ToString();
        }

        var errorCode = Marshal.GetLastWin32Error();
        if (errorCode != SetupApiInterop.ERROR_INSUFFICIENT_BUFFER)
        {
            return string.Empty;
        }

        buffer = new StringBuilder(requiredSize + 1);
        if (!SetupApiInterop.SetupDiGetDeviceInstanceId(deviceInfoSet, ref deviceInfoData, buffer, buffer.Capacity, out _))
        {
            return string.Empty;
        }

        return buffer.ToString();
    }

    private static string GetRegistryStringProperty(nint deviceInfoSet, SetupApiInterop.SP_DEVINFO_DATA deviceInfoData, uint property)
    {
        if (!TryGetRegistryProperty(deviceInfoSet, deviceInfoData, property, out var regType, out var buffer))
        {
            return string.Empty;
        }

        if (regType != SetupApiInterop.REG_SZ || buffer.Length == 0)
        {
            return string.Empty;
        }

        return Encoding.Unicode.GetString(buffer)
            .TrimEnd('\0')
            .Trim();
    }

    private static IReadOnlyList<string> GetRegistryMultiStringProperty(nint deviceInfoSet, SetupApiInterop.SP_DEVINFO_DATA deviceInfoData, uint property)
    {
        if (!TryGetRegistryProperty(deviceInfoSet, deviceInfoData, property, out var regType, out var buffer))
        {
            return [];
        }

        if (regType != SetupApiInterop.REG_MULTI_SZ || buffer.Length == 0)
        {
            return [];
        }

        return Encoding.Unicode.GetString(buffer)
            .Split('\0', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool TryGetRegistryProperty(
        nint deviceInfoSet,
        SetupApiInterop.SP_DEVINFO_DATA deviceInfoData,
        uint property,
        out uint regType,
        out byte[] buffer)
    {
        regType = 0;
        buffer = [];
        uint requiredSize = 0;

        SetupApiInterop.SetupDiGetDeviceRegistryProperty(
            deviceInfoSet,
            ref deviceInfoData,
            property,
            out regType,
            [],
            0,
            out requiredSize);

        var errorCode = Marshal.GetLastWin32Error();

        if (errorCode == SetupApiInterop.ERROR_INVALID_DATA || errorCode == SetupApiInterop.ERROR_NOT_FOUND || requiredSize == 0)
        {
            return false;
        }

        if (errorCode != SetupApiInterop.ERROR_INSUFFICIENT_BUFFER)
        {
            return false;
        }

        buffer = new byte[requiredSize];
        if (!SetupApiInterop.SetupDiGetDeviceRegistryProperty(
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
