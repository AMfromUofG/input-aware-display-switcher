using System.Runtime.InteropServices;
using System.Text;

namespace RawInputPrototype.RawInput;

internal static class SetupApiInterop
{
    public const int ERROR_INSUFFICIENT_BUFFER = 122;
    public const int ERROR_INVALID_DATA = 13;
    public const int ERROR_NOT_FOUND = 1168;

    public const uint SPDRP_DEVICEDESC = 0x00000000;
    public const uint SPDRP_HARDWAREID = 0x00000001;
    public const uint SPDRP_CLASS = 0x00000007;
    public const uint SPDRP_MFG = 0x0000000B;
    public const uint SPDRP_FRIENDLYNAME = 0x0000000C;
    public const uint SPDRP_LOCATION_INFORMATION = 0x0000000D;
    public const uint SPDRP_ENUMERATOR_NAME = 0x00000016;

    public const uint REG_SZ = 1;
    public const uint REG_MULTI_SZ = 7;

    public static readonly nint InvalidHandleValue = new(-1);

    [DllImport("setupapi.dll", SetLastError = true)]
    public static extern nint SetupDiCreateDeviceInfoList(nint classGuid, nint hwndParent);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiDestroyDeviceInfoList(nint deviceInfoSet);

    [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiOpenDeviceInterfaceW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiOpenDeviceInterface(
        nint deviceInfoSet,
        string devicePath,
        uint openFlags,
        ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiGetDeviceInterfaceDetailW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceInterfaceDetail(
        nint deviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
        nint deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize,
        ref uint requiredSize,
        ref SP_DEVINFO_DATA deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiGetDeviceInstanceIdW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceInstanceId(
        nint deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        StringBuilder deviceInstanceId,
        int deviceInstanceIdSize,
        out int requiredSize);

    [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiGetDeviceRegistryPropertyW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceRegistryProperty(
        nint deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        byte[] propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize);

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVICE_INTERFACE_DATA
    {
        public uint cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public nuint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public nuint Reserved;
    }
}
