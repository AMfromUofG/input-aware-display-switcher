using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RawInputPrototype.RawInput;

internal static class RawInputRegistration
{
    private const ushort GenericDesktopControlsUsagePage = 0x01;
    private const ushort MouseUsage = 0x02;
    private const ushort KeyboardUsage = 0x06;

    public static void RegisterKeyboardAndMouse(nint windowHandle)
    {
        var devices = new[]
        {
            new RawInputInterop.RAWINPUTDEVICE
            {
                usUsagePage = GenericDesktopControlsUsagePage,
                usUsage = MouseUsage,
                dwFlags = RawInputInterop.RIDEV_INPUTSINK | RawInputInterop.RIDEV_DEVNOTIFY,
                hwndTarget = windowHandle
            },
            new RawInputInterop.RAWINPUTDEVICE
            {
                usUsagePage = GenericDesktopControlsUsagePage,
                usUsage = KeyboardUsage,
                dwFlags = RawInputInterop.RIDEV_INPUTSINK | RawInputInterop.RIDEV_DEVNOTIFY,
                hwndTarget = windowHandle
            }
        };

        var registrationSucceeded = RawInputInterop.RegisterRawInputDevices(
            devices,
            (uint)devices.Length,
            (uint)Marshal.SizeOf<RawInputInterop.RAWINPUTDEVICE>());

        if (!registrationSucceeded)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}
