using System.Runtime.InteropServices;

namespace InputAwareDisplaySwitcher.Infrastructure.Windows.Input;

internal static class WindowsRawInputInterop
{
    public const uint DeviceNameCommand = 0x20000007;
    public const uint DeviceInfoCommand = 0x2000000b;

    public const uint RawInputMouse = 0;
    public const uint RawInputKeyboard = 1;
    public const uint RawInputHid = 2;

    [DllImport("User32.dll", SetLastError = true)]
    public static extern uint GetRawInputDeviceList(
        nint rawInputDeviceList,
        ref uint deviceCount,
        uint elementSize);

    [DllImport("User32.dll", SetLastError = true, EntryPoint = "GetRawInputDeviceInfoW")]
    public static extern uint GetRawInputDeviceInfo(
        nint deviceHandle,
        uint command,
        nint data,
        ref uint dataSize);

    [DllImport("User32.dll", SetLastError = true, EntryPoint = "GetRawInputDeviceInfoW", CharSet = CharSet.Unicode)]
    public static extern uint GetRawInputDeviceInfo(
        nint deviceHandle,
        uint command,
        ref RawInputDeviceInfo data,
        ref uint dataSize);

    public static string FormatHandle(nint handle)
    {
        return $"0x{handle.ToInt64():X16}";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputDeviceListEntry
    {
        public nint DeviceHandle;
        public uint DeviceType;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputDeviceInfo
    {
        public uint Size;
        public uint DeviceType;
        public RawInputDeviceInfoUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct RawInputDeviceInfoUnion
    {
        [FieldOffset(0)]
        public RawInputMouseInfo Mouse;

        [FieldOffset(0)]
        public RawInputKeyboardInfo Keyboard;

        [FieldOffset(0)]
        public RawInputHidInfo Hid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputMouseInfo
    {
        public uint Id;
        public uint NumberOfButtons;
        public uint SampleRate;
        public bool HasHorizontalWheel;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputKeyboardInfo
    {
        public uint Type;
        public uint SubType;
        public uint KeyboardMode;
        public uint NumberOfFunctionKeys;
        public uint NumberOfIndicators;
        public uint NumberOfKeysTotal;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputHidInfo
    {
        public uint VendorId;
        public uint ProductId;
        public uint VersionNumber;
        public ushort UsagePage;
        public ushort Usage;
    }
}
