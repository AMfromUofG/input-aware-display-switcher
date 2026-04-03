using System.Runtime.InteropServices;
using System.Text;

namespace InputAwareDisplaySwitcher.Infrastructure.Windows.Input;

internal static class WindowsSetupApiInterop
{
    public const int ErrorInsufficientBuffer = 122;
    public const int ErrorInvalidData = 13;
    public const int ErrorNotFound = 1168;

    public const uint PropertyDeviceDescription = 0x00000000;
    public const uint PropertyFriendlyName = 0x0000000C;

    public const uint RegistryString = 1;

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
        ref DeviceInterfaceData interfaceData);

    [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiGetDeviceInterfaceDetailW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceInterfaceDetail(
        nint deviceInfoSet,
        ref DeviceInterfaceData interfaceData,
        nint deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize,
        ref uint requiredSize,
        ref DeviceInfoData deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiGetDeviceInstanceIdW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceInstanceId(
        nint deviceInfoSet,
        ref DeviceInfoData deviceInfoData,
        StringBuilder deviceInstanceId,
        int deviceInstanceIdSize,
        out int requiredSize);

    [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiGetDeviceRegistryPropertyW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceRegistryProperty(
        nint deviceInfoSet,
        ref DeviceInfoData deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        byte[] propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize);

    [StructLayout(LayoutKind.Sequential)]
    public struct DeviceInterfaceData
    {
        public uint Size;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public nuint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DeviceInfoData
    {
        public uint Size;
        public Guid ClassGuid;
        public uint DeviceInstance;
        public nuint Reserved;
    }
}
